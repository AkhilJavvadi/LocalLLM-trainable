# This file contains the script for training the model.
# train.py  (CPU-friendly LoRA for Meta-Llama-3 family, with heartbeats & resume)
import argparse, os, sys, json, time, threading, warnings

def log(msg: str):
    """Logs a message to the console."""
    print(msg, flush=True)

def heartbeat():
    """Prints a heartbeat message every 60 seconds."""
    while True:
        time.sleep(60)
        try:
            print("[heartbeat] still running...", flush=True)
        except Exception:
            break

def load_yaml_cfg(path: str) -> dict:
    """Loads a YAML configuration file."""
    import yaml
    with open(path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f) or {}

def parse_args():
    """Parses the command-line arguments."""
    p = argparse.ArgumentParser()
    p.add_argument("--config", type=str, default=None)
    p.add_argument("--data", type=str, help="jsonl with {prompt,response}")
    # Default to small, CPU-friendly model for POC (you can change to 8B in UI)
    p.add_argument("--base", type=str, default="meta-llama/Llama-3.2-1B-Instruct")
    p.add_argument("--outdir", type=str, default="artifacts")
    p.add_argument("--epochs", type=int, default=3)
    p.add_argument("--lr", type=float, default=4e-4)
    p.add_argument("--bsz", type=int, default=1)           # CPU-safe default
    p.add_argument("--max_length", type=int, default=512)  # CPU-safe default
    p.add_argument("--resume", type=str, default="auto", choices=["auto","never","force"])
    return p.parse_args()

def load_jsonl_rows(path: str):
    """Loads a JSONL file."""
    rows = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            s = line.strip()
            if not s: continue
            rows.append(json.loads(s))
    return rows

def build_dataset(jsonl_path: str, tokenizer, max_length: int):
    """Builds a dataset from a JSONL file."""
    import datasets
    rows = load_jsonl_rows(jsonl_path)

    def to_text(ex):
        prompt = (ex.get("prompt") or "").strip()
        resp   = (ex.get("response") or "").strip()
        return {"text": f"User: {prompt}\nAssistant: {resp}"}

    ds = datasets.Dataset.from_list(list(map(to_text, rows)))

    def tok(batch):
        out = tokenizer(
            batch["text"],
            truncation=True,
            padding="max_length",
            max_length=max_length,
            return_tensors=None,
        )
        # causal LM: labels = input_ids
        out["labels"] = out["input_ids"].copy()
        return out

    return ds.map(tok, batched=True, remove_columns=["text"])

def normalize_base_model_id(s: str) -> str:
    """
    Map common Ollama tags / short names to Hugging Face repo ids.
    If no mapping exists, returns the original (stripped) string.
    """
    if not s:
        return s
    key = s.strip().lower().replace(" ", "")

    aliases = {
        # Llama 3 / 3.2 (add/adjust as you like)
        "llama3:8b": "meta-llama/Meta-Llama-3-8B-Instruct",
        "llama3:instruct": "meta-llama/Meta-Llama-3-8B-Instruct",
        "llama3.2:1b-instruct": "meta-llama/Llama-3.2-1B-Instruct",
        "llama3.2:3b-instruct": "meta-llama/Llama-3.2-3B-Instruct",

        # Qwen examples (optional)
        "qwen2.5:0.5b": "Qwen/Qwen2.5-0.5B-Instruct",
        "qwen2.5:1.5b": "Qwen/Qwen2.5-1.5B-Instruct",
    }
    return aliases.get(key, s.strip())

def main():
    """The main function."""
    warnings.filterwarnings("ignore", category=UserWarning, module="torch.utils.data.dataloader")

    args = parse_args()
    if args.config:
        cfg = load_yaml_cfg(args.config)
        data_path   = cfg.get("dataset_path") or args.data
        base_model_raw  = (cfg.get("base_model") or args.base).strip()
        epochs      = int(cfg.get("epochs", args.epochs))
        lr          = float(cfg.get("learning_rate", args.lr))
        outdir      = cfg.get("output_dir", args.outdir)
        bsz         = int(cfg.get("batch_size", cfg.get("bsz", args.bsz)))
        max_length  = int(cfg.get("max_length", args.max_length))
    else:
        data_path   = args.data
        base_model_raw  = args.base.strip()
        epochs, lr, outdir, bsz, max_length = args.epochs, args.lr, args.outdir, args.bsz, args.max_length

    # NEW: normalize Ollama tags -> HF repo ids
    base_model = normalize_base_model_id(base_model_raw)

    # (optional) nice logging line
    log(f" base_model   : {base_model_raw}  ->  {base_model}")



    if not data_path or not os.path.exists(data_path):
        log(f"[ERROR] dataset_path not found: {data_path}")
        return 1

    os.makedirs(outdir, exist_ok=True)
    hf_out = os.path.join(outdir, "hf_out")
    os.makedirs(hf_out, exist_ok=True)

    log("="*53)
    log("[train.py] LoRA fine-tune - Meta-Llama-3 family")
    log(f" dataset_path : {data_path}")
    log(f" base_model   : {base_model}")
    log(f" epochs       : {epochs}")
    log(f" learning_rate: {lr}")
    log(f" batch_size   : {bsz}")
    log(f" max_length   : {max_length}")
    log(f" output_dir   : {outdir}")
    log("="*53)

    # # Start a heartbeat thread so you always see activity in train.log
    # t = threading.Thread(target=heartbeat, daemon=True)
    # t.start()

    import torch
    if not torch.cuda.is_available():
        try:
            torch.set_num_threads(max(1, os.cpu_count() or 1))
        except Exception:
            pass

    from transformers import (
        AutoTokenizer, AutoModelForCausalLM,
        TrainingArguments, Trainer, DataCollatorForLanguageModeling
    )
    from peft import LoraConfig, get_peft_model

    log("STEP 1/6: Loading tokenizer...")
    tokenizer = AutoTokenizer.from_pretrained(
        base_model,
        use_fast=True,
        token=os.getenv("HUGGINGFACE_HUB_TOKEN", None)
    )
    if tokenizer.pad_token is None:
        tokenizer.pad_token = tokenizer.eos_token

    log("STEP 2/6: Building & tokenizing dataset...")
    train_ds = build_dataset(data_path, tokenizer, max_length)

    log("STEP 3/6: Loading base model (this can take a while the first time)...")
    model = AutoModelForCausalLM.from_pretrained(
        base_model,
        token=os.getenv("HUGGINGFACE_HUB_TOKEN", None),
        low_cpu_mem_usage=True,
        torch_dtype=torch.float32 if not torch.cuda.is_available() else None,
        device_map=None,
    )
    model.config.pad_token_id = tokenizer.pad_token_id

    try:
        model.gradient_checkpointing_enable()
    except Exception:
        pass

    log("STEP 4/6: Applying LoRA adapters...")
    lora_cfg = LoraConfig(
        r=8, lora_alpha=16, lora_dropout=0.05,
        bias="none", task_type="CAUSAL_LM",
        target_modules=["q_proj","k_proj","v_proj","o_proj","gate_proj","up_proj","down_proj"]
    )
    model = get_peft_model(model, lora_cfg)

    log("STEP 5/6: Starting training...")
    use_cuda = torch.cuda.is_available()
    args_train = TrainingArguments(
        output_dir=hf_out,
        per_device_train_batch_size=bsz,
        num_train_epochs=epochs,
        learning_rate=lr,
        gradient_accumulation_steps=1,
        logging_steps=5,
        save_strategy="epoch",
        save_total_limit=2,
        report_to=[],
        dataloader_pin_memory=use_cuda,
        fp16=use_cuda,
        bf16=False,
    )
    trainer = Trainer(
        model=model,
        args=args_train,
        train_dataset=train_ds,
        data_collator=DataCollatorForLanguageModeling(tokenizer=tokenizer, mlm=False),
    )

    # Auto-resume if a checkpoint exists
    ckpt = None
    if os.path.isdir(hf_out):
        ckpts = [d for d in os.listdir(hf_out) if d.startswith("checkpoint-")]
        if ckpts:
            ckpts.sort(key=lambda n: int(n.split("-")[-1]) if "-" in n else -1)
            ckpt = os.path.join(hf_out, ckpts[-1])

    if ckpt:
        log(f"[resume] Found checkpoint: {ckpt}")
        trainer.train(resume_from_checkpoint=ckpt)
    else:
        trainer.train()

    log("STEP 6/6: Saving LoRA adapter...")
    model.save_pretrained(outdir, safe_serialization=True)

    # Ensure Ollama can consume the adapter:
    adapter = os.path.join(outdir, "adapter_model.safetensors")
    cfg     = os.path.join(outdir, "adapter_config.json")
    if not os.path.exists(adapter):
        log("[ERROR] adapter_model.safetensors not found after save_pretrained.")
        return 1
    if not os.path.exists(cfg):
        fallback = {
            "base_model_name_or_path": base_model,
            "bias": "none",
            "inference_mode": True,
            "lora_alpha": 16, "lora_dropout": 0.05, "peft_type": "LORA",
            "r": 8, "target_modules": ["q_proj","k_proj","v_proj","o_proj","gate_proj","up_proj","down_proj"],
            "task_type": "CAUSAL_LM"
        }
        with open(cfg, "w", encoding="utf-8") as f:
            json.dump(fallback, f, ensure_ascii=False, indent=2)

    # Write Modelfile for Ollama. You can switch FROM to llama3:8b later.
    modelfile = os.path.join(outdir, "Modelfile")
    with open(modelfile, "w", encoding="utf-8") as f:
        f.write(
            'FROM llama3:8b\n'
            'ADAPTER ./adapter_model.safetensors\n\n'
            'SYSTEM "You are a concise E-commerce Returns & Refunds assistant. Stick strictly to policy."\n\n'
            'TEMPLATE "User: {{ {{ .Prompt }} }}\nAssistant:"\n\n'
            'PARAMETER temperature 0.2\n'
            'PARAMETER num_predict 256\n'
        )

    log(f"[OK] Artifacts ready in: {outdir}")
    return 0

if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        # Write a last-ditch error marker so the backend can show something
        try:
            # attempt to figure out outdir from args
            outdir = None
            for i, a in enumerate(sys.argv):
                if a == "--outdir" and i+1 < len(sys.argv):
                    outdir = sys.argv[i+1]
                    break
            if outdir:
                os.makedirs(outdir, exist_ok=True)
                with open(os.path.join(outdir, "error.txt"), "a", encoding="utf-8") as ef:
                    ef.write(str(e) + "\n")
        except Exception:
            pass
        raise
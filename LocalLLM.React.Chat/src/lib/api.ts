export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

export type ChatRequest = { Prompt: string; Model?: string };
export type ModelInfo = { name: string };

// ============ CHAT (streams) ============
export async function* streamChat(req: ChatRequest, signal?: AbortSignal): AsyncGenerator<string, void, void> {
  const res = await fetch(`${API_BASE}/api/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
    signal,
  });
  if (!res.ok || !res.body) {
    const text = await res.text().catch(() => '');
    throw new Error(`Chat failed: ${res.status} ${res.statusText}${text ? ` — ${text}` : ''}`);
  }
  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    yield decoder.decode(value, { stream: true });
  }
}

// ============ MODELS ============
export async function listModels(): Promise<ModelInfo[]> {
  try {
    const res = await fetch(`${API_BASE}/api/models`, { headers: { 'Accept': 'application/json' } });
    if (!res.ok) throw new Error(`listModels failed ${res.status}`);
    const data = await res.json();

    let names: string[] = [];
    if (Array.isArray(data)) names = data.map((x: any) => String(x));
    else if (Array.isArray((data as any)?.models)) names = (data as any).models.map((m: any) => String(m.name ?? m));

    if (names.length === 0) names = ['my-faq-model-local', 'qwen2.5:0.5b']; // fallback
    return names.map(name => ({ name }));
  } catch {
    return [{ name: 'my-faq-model-local' }, { name: 'qwen2.5:0.5b' }];
  }
}

// ============ DATASETS ============
export type DatasetListItem = { Id: string; Path: string; Count: number; CreatedAt: string };
export type DatasetUploadResponse = DatasetListItem;

export async function listDatasets(): Promise<DatasetListItem[]> {
  const res = await fetch(`${API_BASE}/api/dataset`);
  if (!res.ok) throw new Error(`listDatasets failed: ${res.status}`);
  const raw = await res.json();
  // Normalize casing if the backend ever returns camelCase
  return (raw as any[]).map((d: any) => ({
    Id: d.Id ?? d.id,
    Path: d.Path ?? d.path,
    Count: d.Count ?? d.count,
    CreatedAt: d.CreatedAt ?? d.createdAt,
  }));
}

export async function uploadDataset(file: File): Promise<DatasetUploadResponse> {
  const form = new FormData();
  form.append('file', file);
  const res = await fetch(`${API_BASE}/api/dataset`, { method: 'POST', body: form });
  if (!res.ok) throw new Error(`upload failed: ${res.status}`);
  const d = await res.json();
  return {
    Id: d.Id ?? d.id,
    Path: d.Path ?? d.path,
    Count: d.Count ?? d.count,
    CreatedAt: d.CreatedAt ?? d.createdAt,
  };
}

// ============ TRAIN ============
export type TrainLaunchRequest = {
  DatasetId: string;
  BaseModel: string;
  Epochs: number;
  LearningRate: number;
};

export type TrainJob = { jobId: string; state: string; startedAt?: string };
export type TrainStatus = { jobId: string; state: string; logsTail?: string; artifactPath?: string };

export async function launchTraining(req: TrainLaunchRequest): Promise<TrainJob> {
  const res = await fetch(`${API_BASE}/api/train`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(`launchTraining failed: ${res.status} ${text}`);
  }
  const raw = await res.json();
  const jobId = String(raw.jobId ?? raw.JobId ?? raw.id ?? raw.ID ?? '');
  if (!jobId) throw new Error(`launchTraining: server did not return jobId. Raw: ${JSON.stringify(raw)}`);
  const state = String(raw.state ?? raw.State ?? 'Running');
  const startedAt = raw.startedAt ?? raw.StartedAt;
  return { jobId, state, startedAt };
}

export async function getTrainingStatus(jobId: string): Promise<TrainStatus> {
  if (!jobId) throw new Error('getTrainingStatus: jobId is empty');

  // Primary in-memory endpoint
  let res = await fetch(`${API_BASE}/api/train/${encodeURIComponent(jobId)}`);
  if (!res.ok) {
    // Fallback: recovery endpoint that reads disk
    res = await fetch(`${API_BASE}/api/train/recover/${encodeURIComponent(jobId)}`);
  }
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(`getTrainingStatus failed: ${res.status} ${text}`);
  }
  const raw = await res.json();
  return {
    jobId: String(raw.jobId ?? raw.JobId ?? jobId),
    state: String(raw.state ?? raw.State ?? 'Running'),
    logsTail: raw.logsTail ?? raw.LogsTail ?? '',
    artifactPath: raw.artifactPath ?? raw.ArtifactPath ?? undefined,
  };
}

// ============ REGISTER ============
export type RegisterModelResponse = {
  modelName: string;
  artifactsDir: string;
  modelfile: string;
  exitCode: number;
  stdout: string;
  stderr: string;
};

export async function registerModel(jobId: string, modelName: string): Promise<RegisterModelResponse> {
  const res = await fetch(`${API_BASE}/api/model/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ JobId: jobId, ModelName: modelName }),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `registerModel failed: ${res.status}`);
  }
  return await res.json();
}


// 2nd ----------------------------------------------------------------------------

// export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

// export type ChatRequest = { Prompt: string; Model?: string };
// export type ModelInfo = { name: string };

// export async function* streamChat(req: ChatRequest, signal?: AbortSignal): AsyncGenerator<string, void, void> {
//   const res = await fetch(`${API_BASE}/api/chat`, {
//     method: 'POST',
//     headers: { 'Content-Type': 'application/json' },
//     body: JSON.stringify(req),
//     signal,
//   });
//   if (!res.ok || !res.body) {
//     const text = await res.text().catch(() => '');
//     throw new Error(`Chat failed: ${res.status} ${res.statusText}${text ? ` — ${text}` : ''}`);
//   }
//   const reader = res.body.getReader();
//   const decoder = new TextDecoder();
//   while (true) {
//     const { done, value } = await reader.read();
//     if (done) break;
//     yield decoder.decode(value, { stream: true });
//   }
// }

// export async function listModels(): Promise<ModelInfo[]> {
//   try {
//     const res = await fetch(`${API_BASE}/api/models`, { headers: { 'Accept': 'application/json' } });
//     if (!res.ok) throw new Error(`listModels failed ${res.status}`);
//     const data = await res.json();

//     let names: string[] = [];
//     if (Array.isArray(data)) names = data.map((x: any) => String(x));
//     else if (Array.isArray((data as any)?.models)) names = (data as any).models.map((m: any) => String(m.name ?? m));

//     // Fallback if API returns an empty array
//     if (names.length === 0) names = ['my-faq-model-local', 'qwen2.5:0.5b'];

//     return names.map(name => ({ name }));
//   } catch {
//     // Fallback if the request fails
//     return [{ name: 'my-faq-model-local' }, { name: 'qwen2.5:0.5b' }];
//   }
// }

// 1st ----------------------------------------------------------------------------

// // export const API_BASE = (import.meta.env.VITE_API_BASE as string) || 'http://localhost:5000';
// export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

// export type ChatRequest = { Prompt: string; Model?: string };
// export type ModelInfo = { name: string };

// export async function* streamChat(req: ChatRequest, signal?: AbortSignal): AsyncGenerator<string, void, void> {
//   const res = await fetch(`${API_BASE}/api/chat`, {
//     method: 'POST',
//     headers: { 'Content-Type': 'application/json' },
//     body: JSON.stringify(req),
//     signal,
//   });
//   if (!res.ok || !res.body) {
//     const text = await res.text().catch(() => '');
//     throw new Error(`Chat failed: ${res.status} ${res.statusText}${text ? ` — ${text}` : ''}`);
//   }
//   const reader = res.body.getReader();
//   const decoder = new TextDecoder();
//   while (true) {
//     const { done, value } = await reader.read();
//     if (done) break;
//     yield decoder.decode(value, { stream: true });
//   }
// }

// export async function listModels(): Promise<ModelInfo[]> {
//   try {
//     const res = await fetch(`${API_BASE}/api/models`);
//     if (!res.ok) throw new Error(`listModels failed ${res.status}`);
//     const data = await res.json();
//     if (Array.isArray(data)) {
//       return data.map((x: any) => ({ name: String(x) }));
//     }
//     if (Array.isArray((data as any)?.models)) {
//       return (data as any).models.map((m: any) => ({ name: String(m.name ?? m) }));
//     }
//     return [];
//   } catch {
//     // Fallback if /api/models doesn’t exist on your backend
//     return [{ name: 'my-faq-model-local' }, { name: 'qwen2.5:0.5b' }];
//   }
// }

import React, { useEffect, useMemo, useRef, useState } from 'react'
import {
  listDatasets, uploadDataset,
  launchTraining, getTrainingStatus,
  registerModel, listModels,
  type DatasetListItem
} from '../lib/api'

type Props = {
  onModelRegistered?: (modelName: string) => void
  onRefreshModels?: () => Promise<void>
  currentModel?: string
}

export default function TrainRegister({ onModelRegistered, onRefreshModels }: Props) {
  // datasets
  const [datasets, setDatasets] = useState<DatasetListItem[]>([])
  const [uploading, setUploading] = useState(false)
  const [selectedDatasetId, setSelectedDatasetId] = useState('')

  // train
  const [baseModel, setBaseModel] = useState('qwen2.5:0.5b')
  const [epochs, setEpochs] = useState(3)
  const [lr, setLr] = useState(0.0002)
  const [launching, setLaunching] = useState(false)
  const [jobId, setJobId] = useState('')
  const [status, setStatus] = useState<'Idle' | 'Running' | 'Succeeded' | 'Failed'>('Idle')
  const [logs, setLogs] = useState('')
  const pollRef = useRef<number | null>(null)

  // register
  const [modelName, setModelName] = useState('my-faq-model-local')
  const [regging, setRegging] = useState(false)
  const [regOut, setRegOut] = useState<string>('')

  async function refreshDatasets() {
    try {
      const list = await listDatasets()
      setDatasets(list)
      if (!selectedDatasetId && list.length) setSelectedDatasetId(list[list.length - 1].Id)
    } catch (e: any) {
      console.error(e)
    }
  }

  useEffect(() => {
    refreshDatasets()
    return () => { if (pollRef.current) window.clearInterval(pollRef.current) }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const canTrain = useMemo(() =>
    !!selectedDatasetId && !!baseModel && epochs > 0 && lr > 0, [selectedDatasetId, baseModel, epochs, lr])

  async function onUpload(ev: React.ChangeEvent<HTMLInputElement>) {
    const f = ev.target.files?.[0]
    if (!f) return
    setUploading(true)
    try {
      const res = await uploadDataset(f)
      await refreshDatasets()
      setSelectedDatasetId(res.Id)
    } catch (e: any) {
      alert(`Upload failed: ${e?.message || e}`)
    } finally {
      setUploading(false)
      ev.target.value = ''
    }
  }

  async function onLaunch() {
    if (!canTrain) return
    setLaunching(true)
    setLogs('')
    setRegOut('')
    try {
      const job = await launchTraining({
        DatasetId: selectedDatasetId,
        BaseModel: baseModel,
        Epochs: epochs,
        LearningRate: lr
      })
      setJobId(job.jobId)
      setStatus('Running')

      if (pollRef.current) window.clearInterval(pollRef.current)
      pollRef.current = window.setInterval(async () => {
        try {
          const st = await getTrainingStatus(job.jobId)
          setLogs(st.logsTail || '')
          if (st.state === 'Succeeded' || st.state === 'Failed') {
            setStatus(st.state as any)
            if (pollRef.current) { window.clearInterval(pollRef.current); pollRef.current = null }
          }
        } catch (e: any) {
          console.error(e)
        }
      }, 1500)
    } catch (e: any) {
      alert(`Launch failed: ${e?.message || e}`)
    } finally {
      setLaunching(false)
    }
  }

  async function onRegister() {
    if (!jobId || status !== 'Succeeded' || !modelName.trim()) return
    setRegging(true)
    setRegOut('')
    try {
      const out = await registerModel(jobId, modelName.trim())
      setRegOut(`ExitCode: ${out.exitCode}\n${out.stdout || out.stderr}`)
      try {
        if (onRefreshModels) await onRefreshModels()
        else await listModels()
      } catch {}
      onModelRegistered?.(modelName.trim())
    } catch (e: any) {
      setRegOut(`❌ ${e?.message || e}`)
    } finally {
      setRegging(false)
    }
  }

  return (
    <div className="panel">
      {/* 1. Upload */}
      <section className="section">
        <h3>1) Upload dataset</h3>
        <div className="row" style={{marginTop:6}}>
          <input type="file" onChange={onUpload} disabled={uploading} />
          <span className="hint">JSONL format. The latest upload will be auto-selected.</span>
        </div>
        <div className="row" style={{marginTop:10}}>
          <label className="badge">Datasets</label>
          <select className="select" value={selectedDatasetId} onChange={e => setSelectedDatasetId(e.target.value)}>
            {datasets.map(d => (
              <option key={d.Id} value={d.Id}>{d.Id} — {d.Count} rows</option>
            ))}
          </select>
          <button className="button" onClick={refreshDatasets}>Refresh</button>
        </div>
      </section>

      {/* 2. Train */}
      <section className="section">
        <h3>2) Launch training</h3>
        <div className="row" style={{marginTop:6}}>
          <label className="badge">Base model</label>
          <input className="input" value={baseModel} onChange={e => setBaseModel(e.target.value)} />
          <label className="badge">Epochs</label>
          <input className="input" type="number" value={epochs} min={1}
                 onChange={e => setEpochs(parseInt(e.target.value || '1', 10))} />
          <label className="badge">LR</label>
          <input className="input" type="number" step="0.0001" value={lr}
                 onChange={e => setLr(parseFloat(e.target.value || '0.0001'))} />
          <button className="button" disabled={!canTrain || launching} onClick={onLaunch}>
            {launching ? 'Starting…' : 'Start training'}
          </button>
        </div>
        <div className="row" style={{marginTop:8}}>
          <span className="badge">Job: {jobId || '—'}</span>
          <span className={`badge ${status==='Succeeded'?'ok':status==='Failed'?'err':'warn'}`}>Status: {status}</span>
        </div>
        <div style={{marginTop:8}} className="logbox">{logs || 'Logs will appear here…'}</div>
      </section>

      {/* 3. Register */}
      <section className="section">
        <h3>3) Register as model</h3>
        <div className="row" style={{marginTop:8}}>
          <label className="badge">Model name</label>
          <input className="input" value={modelName} onChange={e => setModelName(e.target.value)} />
          <button className="button" disabled={status !== 'Succeeded' || regging} onClick={onRegister}>
            {regging ? 'Registering…' : 'Register'}
          </button>
          <button className="button" disabled={!modelName.trim()} onClick={() => onModelRegistered?.(modelName.trim())}>
            Use in Chat
          </button>
        </div>
        <div style={{marginTop:8}} className="logbox">{regOut || 'Output will appear here…'}</div>
      </section>
    </div>
  )
}



// 1st -------------------------------------------------------------------------------------------
// import React, { useEffect, useMemo, useRef, useState } from 'react'
// import {
//   listDatasets, uploadDataset,
//   launchTraining, getTrainingStatus,
//   registerModel, listModels,
//   type DatasetListItem
// } from '../lib/api'

// type Props = {
//   onModelRegistered?: (modelName: string) => void
//   onRefreshModels?: () => Promise<void>
//   currentModel?: string
// }

// export default function TrainRegister({ onModelRegistered, onRefreshModels, currentModel }: Props) {
//   // datasets
//   const [datasets, setDatasets] = useState<DatasetListItem[]>([])
//   const [uploading, setUploading] = useState(false)
//   const [selectedDatasetId, setSelectedDatasetId] = useState('')

//   // train
//   const [baseModel, setBaseModel] = useState('qwen2.5:0.5b')
//   const [epochs, setEpochs] = useState(3)
//   const [lr, setLr] = useState(0.0002)
//   const [launching, setLaunching] = useState(false)
//   const [jobId, setJobId] = useState('')
//   const [status, setStatus] = useState<'Idle' | 'Running' | 'Succeeded' | 'Failed'>('Idle')
//   const [logs, setLogs] = useState('')
//   const pollRef = useRef<number | null>(null)

//   // register
//   const [modelName, setModelName] = useState('my-faq-model-local')
//   const [regging, setRegging] = useState(false)
//   const [regOut, setRegOut] = useState<string>('')

//   async function refreshDatasets() {
//     try {
//       const list = await listDatasets()
//       setDatasets(list)
//       if (!selectedDatasetId && list.length) setSelectedDatasetId(list[list.length - 1].Id) // newest
//     } catch (e: any) {
//       console.error(e)
//     }
//   }

//   useEffect(() => {
//     refreshDatasets()
//     return () => { if (pollRef.current) window.clearInterval(pollRef.current) }
//     // eslint-disable-next-line react-hooks/exhaustive-deps
//   }, [])

//   const canTrain = useMemo(() =>
//     !!selectedDatasetId && !!baseModel && epochs > 0 && lr > 0, [selectedDatasetId, baseModel, epochs, lr])

//   async function onUpload(ev: React.ChangeEvent<HTMLInputElement>) {
//     const f = ev.target.files?.[0]
//     if (!f) return
//     setUploading(true)
//     try {
//       const res = await uploadDataset(f)
//       await refreshDatasets()
//       setSelectedDatasetId(res.Id)
//     } catch (e: any) {
//       alert(`Upload failed: ${e?.message || e}`)
//     } finally {
//       setUploading(false)
//       ev.target.value = ''
//     }
//   }

//   async function onLaunch() {
//     if (!canTrain) return
//     setLaunching(true)
//     setLogs('')
//     setRegOut('')
//     try {
//       const job = await launchTraining({
//         DatasetId: selectedDatasetId,
//         BaseModel: baseModel,
//         Epochs: epochs,
//         LearningRate: lr
//       })
//       setJobId(job.jobId)
//       setStatus('Running')
//       // poll
//       if (pollRef.current) window.clearInterval(pollRef.current)
//       pollRef.current = window.setInterval(async () => {
//         try {
//           const st = await getTrainingStatus(job.jobId)
//           // logsTail comes as a single string; show last ~120 lines already from API
//           setLogs(st.logsTail || '')
//           if (st.state === 'Succeeded' || st.state === 'Failed') {
//             setStatus(st.state as any)
//             if (pollRef.current) { window.clearInterval(pollRef.current); pollRef.current = null }
//           }
//         } catch (e: any) {
//           console.error(e);
//           setLogs(`Polling error: ${e?.message || e}\n` + (logs || ''));
//         }
//       }, 1500)
//     } catch (e: any) {
//       alert(`Launch failed: ${e?.message || e}`)
//     } finally {
//       setLaunching(false)
//     }
//   }

//   async function onRegister() {
//     if (!jobId || status !== 'Succeeded' || !modelName.trim()) return
//     setRegging(true)
//     setRegOut('')
//     try {
//       const out = await registerModel(jobId, modelName.trim())
//       setRegOut(`ExitCode: ${out.exitCode}\n${out.stdout || out.stderr}`)
//       // refresh available models for the app dropdown
//       try {
//         if (onRefreshModels) await onRefreshModels()
//         else await listModels() // no-op but forces path
//       } catch {}
//       if (onModelRegistered) onModelRegistered(modelName.trim())
//     } catch (e: any) {
//       setRegOut(`❌ ${e?.message || e}`)
//     } finally {
//       setRegging(false)
//     }
//   }

//   return (
//     <div className="panel">
//       <section className="section">
//         <h3>1) Upload dataset</h3>
//         <input type="file" onChange={onUpload} disabled={uploading} />
//         <div className="hint">Accepts your JSONL file. Latest upload auto-selected.</div>
//         <div className="row">
//           <label className="label">Datasets</label>
//           <select className="select" value={selectedDatasetId} onChange={e => setSelectedDatasetId(e.target.value)}>
//             {datasets.map(d => (
//               <option key={d.Id} value={d.Id}>
//                 {d.Id} — {d.Count} rows
//               </option>
//             ))}
//           </select>
//           <button className="button" onClick={refreshDatasets}>Refresh</button>
//         </div>
//       </section>

//       <section className="section">
//         <h3>2) Launch training</h3>
//         <div className="row">
//           <label className="label">Base model</label>
//           <input className="input" value={baseModel} onChange={e => setBaseModel(e.target.value)} />
//           <label className="label">Epochs</label>
//           <input className="input" type="number" value={epochs} min={1}
//                  onChange={e => setEpochs(parseInt(e.target.value || '1', 10))} />
//           <label className="label">LR</label>
//           <input className="input" type="number" step="0.0001" value={lr}
//                  onChange={e => setLr(parseFloat(e.target.value || '0.0001'))} />
//           <button className="button" disabled={!canTrain || launching} onClick={onLaunch}>
//             {launching ? 'Starting…' : 'Start training'}
//           </button>
//         </div>

//         <div className="hint">Job: {jobId || '—'} | Status: <b>{status}</b></div>
//         <pre className="logbox">{logs || 'Logs will appear here…'}</pre>
//       </section>

//       <section className="section">
//         <h3>3) Register as model</h3>
//         <div className="row">
//           <label className="label">Model name</label>
//           <input className="input" value={modelName} onChange={e => setModelName(e.target.value)} />
//           <button className="button" disabled={status !== 'Succeeded' || regging} onClick={onRegister}>
//             {regging ? 'Registering…' : 'Register'}
//           </button>
//           <button className="button" disabled={!modelName.trim()} onClick={() => onModelRegistered?.(modelName.trim())}>
//             Use in Chat
//           </button>
//         </div>
//         <pre className="logbox">{regOut || 'Output will appear here…'}</pre>
//         <div className="hint">Tip: After registering, open the Chat tab and select the model in the dropdown.</div>
//       </section>
//     </div>
//   )
// }

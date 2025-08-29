// This file contains the TrainRegister component.
import React, { useState, useEffect } from 'react'
import { uploadDataset, launchTrain, trainStatus, registerModel, listDatasets } from '../lib/api'

interface Props {
  currentModel: string
  onModelRegistered: (name: string) => void
  onRefreshModels: () => void
}
type JobState = 'idle' | 'uploading' | 'launching' | 'training' | 'registering' | 'done' | 'error'
type Dataset = { id: string, path: string, count: number, createdAt: string }
/// <summary>
/// The TrainRegister component.
/// </summary>
export default function TrainRegister({ currentModel, onModelRegistered, onRefreshModels }: Props) {
  const [file, setFile] = useState<File | null>(null)
  const [datasets, setDatasets] = useState<Dataset[]>([])
  const [selectedDataset, setSelectedDataset] = useState('')
  const [baseModel, setBaseModel] = useState('qwen2.5:0.5b')
  const [jobId, setJobId] = useState('')
  const [jobState, setJobState] = useState<JobState>('idle')
  const [logs, setLogs] = useState('')
  const [error, setError] = useState('')
  const [modelName, setModelName] = useState('')

  useEffect(() => {
    async function fetchDatasets() {
      try {
        const data = await listDatasets()
        setDatasets(data)
        if (data.length > 0) setSelectedDataset(data[0].id)
      } catch (err: any) { setError(`Failed to load datasets: ${err.message}`) }
    }
    fetchDatasets()
  }, [])

  useEffect(() => {
    if (jobState !== 'training' || !jobId) return

    const timer = setInterval(async () => {
      try {
        const status = await trainStatus(jobId)
        setLogs(status.logsTail)
        if (status.state === 'Succeeded') {
          setJobState('done')
          setModelName(`${baseModel.split(':')[0]}-ft-${jobId.slice(-4)}`)
          clearInterval(timer)
        } else if (status.state === 'Failed') {
          setJobState('error')
          setError('Training failed. See logs for details.')
          clearInterval(timer)
        }
      } catch (err: any) {
        setJobState('error')
        setError(`Error fetching status: ${err.message}`)
        clearInterval(timer)
      }
    }, 2000)

    return () => clearInterval(timer)
  }, [jobState, jobId])
  /// <summary>
  /// Handles uploading a dataset.
  /// </summary>
  async function handleUpload() {
    if (!file) return
    setJobState('uploading')
    setError('')
    try {
      const resp = await uploadDataset(file)
      setDatasets([resp, ...datasets])
      setSelectedDataset(resp.id)
    } catch (err: any) { setError(`Upload failed: ${err.message}`) }
    setJobState('idle')
  }
  /// <summary>
  /// Handles launching a training job.
  /// </summary>
  async function handleLaunch() {
    if (!selectedDataset) return
    setJobState('launching')
    setError('')
    try {
      const resp = await launchTrain(selectedDataset, baseModel)
      setJobId(resp.jobId)
      setJobState('training')
    } catch (err: any) {
      setError(`Launch failed: ${err.message}`)
      setJobState('error')
    }
  }
  /// <summary>
  /// Handles registering a model.
  /// </summary>
  async function handleRegister() {
    if (!jobId || !modelName) return
    setJobState('registering')
    setError('')
    try {
      await registerModel(jobId, modelName)
      onRefreshModels()
      onModelRegistered(modelName)
    } catch (err: any) {
      setError(`Registration failed: ${err.message}`)
      setJobState('error')
    }
  }

  return (
    <div className="main">
      <div className="panel">
        <div className="section">
          <h3>1. Upload Dataset (JSONL)</h3>
          <div className="row">
            <input type="file" accept=".jsonl" onChange={e => setFile(e.target.files?.[0] || null)} />
            <button className="button" onClick={handleUpload} disabled={!file || jobState !== 'idle'}>
              Upload
            </button>
          </div>
        </div>

        <div className="section">
          <h3>2. Launch Training Job</h3>
          <div className="row">
            <select className="select" value={selectedDataset} onChange={e => setSelectedDataset(e.target.value)}>
              {datasets.map(d => <option key={d.id} value={d.id}>{d.path} ({d.count} rows)</option>)}
            </select>
            <input className="input" value={baseModel} onChange={e => setBaseModel(e.target.value)} />
            <button className="button" onClick={handleLaunch} disabled={!selectedDataset || jobState !== 'idle'}>
              Launch
            </button>
          </div>
          <div className="hint">Base model can be any Ollama model ID, e.g., qwen2.5:0.5b, llama3:8b</div>
        </div>

        {jobId && (
          <div className="section">
            <h3>3. Monitor & Register</h3>
            <div className="row">
              <div className="badge">Job ID: {jobId}</div>
              <div className={`badge ${jobState === 'done' ? 'ok' : jobState === 'error' ? 'err' : 'warn'}`}>
                Status: {jobState}
              </div>
            </div>
            <div className="logbox">{logs}</div>

            {jobState === 'done' && (
              <div className="row" style={{ marginTop: 10 }}>
                <input className="input" value={modelName} onChange={e => setModelName(e.target.value)} />
                <button className="button" onClick={handleRegister} disabled={!modelName}>
                  Register Model
                </button>
              </div>
            )}
          </div>
        )}

        {error && <div className="badge err">Error: {error}</div>}
      </div>
    </div>
  )
}
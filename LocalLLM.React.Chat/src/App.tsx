import React, { useEffect, useState } from 'react'
import ChatWindow from './components/ChatWindow'
import TrainRegister from './components/TrainRegister'
import { listModels } from './lib/api'

type Tab = 'chat' | 'train'

export default function App() {
  const [models, setModels] = useState<{name: string}[]>([])
  const [model, setModel] = useState('')
  const [tab, setTab] = useState<Tab>('chat')

  async function refreshModels() {
    const list = await listModels()
    setModels(list)
    if (!model && list.length) setModel(list[0].name)
  }

  useEffect(() => { refreshModels() }, [])

  return (
    <div className="app">
      <header className="header">
        <div className="brand">
          <div className="brand-badge">L</div>
          <div>LocalLLM • Studio</div>
        </div>

        <nav className="tabs">
          <button className={`tab ${tab==='chat' ? 'active' : ''}`} onClick={() => setTab('chat')}>Chat</button>
          <button className={`tab ${tab==='train' ? 'active' : ''}`} onClick={() => setTab('train')}>Train &amp; Register</button>
        </nav>

        <div style={{display:'flex', gap:10, alignItems:'center', justifySelf:'end'}}>
          {tab === 'chat' && (
            <>
              <label style={{fontSize:13, color:'#9aa3b2'}}>Model</label>
              <select className="select" value={model} onChange={e => setModel(e.target.value)}>
                {models.map(m => <option key={m.name} value={m.name}>{m.name}</option>)}
              </select>
            </>
          )}
          <button className="button" onClick={refreshModels}>Refresh models</button>
        </div>
      </header>

      <div className="card">
        <div className="card-body">
          {tab === 'chat'
            ? <ChatWindow model={model} />
            : <TrainRegister
                currentModel={model}
                onModelRegistered={(name) => { setModel(name); setTab('chat'); }}
                onRefreshModels={refreshModels}
              />
          }
        </div>
      </div>

      <div className="footer">
        Frontend talks to ASP.NET API at <code>{import.meta.env.VITE_API_BASE || 'http://localhost:5000'}</code>
      </div>
    </div>
  )
}


// 2nd ----------------------------------------------------------------------------
// import React, { useEffect, useState } from 'react'
// import ChatWindow from './components/ChatWindow'
// import TrainRegister from './components/TrainRegister'
// import { listModels } from './lib/api'

// type Tab = 'chat' | 'train'

// export default function App() {
//   const [models, setModels] = useState<{name: string}[]>([])
//   const [model, setModel] = useState('')
//   const [tab, setTab] = useState<Tab>('chat')

//   async function refreshModels() {
//     const list = await listModels()
//     setModels(list)
//     if (!model && list.length) setModel(list[0].name)
//   }

//   useEffect(() => {
//     refreshModels()
//     // eslint-disable-next-line react-hooks/exhaustive-deps
//   }, [])

//   return (
//     <div className="app">
//       <header className="header">
//         <h1>LocalLLM — Chat</h1>
//         <nav className="tabs">
//           <button className={`tab ${tab==='chat' ? 'active' : ''}`} onClick={() => setTab('chat')}>Chat</button>
//           <button className={`tab ${tab==='train' ? 'active' : ''}`} onClick={() => setTab('train')}>Train & Register</button>
//         </nav>
//         <span className="spacer" />
//         {tab === 'chat' && (
//           <label>
//             <span style={{marginRight:8}}>Model</span>
//             <select className="select" value={model} onChange={e => setModel(e.target.value)}>
//               {models.map(m => <option key={m.name} value={m.name}>{m.name}</option>)}
//             </select>
//           </label>
//         )}
//         <button className="button" onClick={refreshModels}>Refresh models</button>
//       </header>

//       {tab === 'chat' ? (
//         <ChatWindow model={model} />
//       ) : (
//         <TrainRegister
//           currentModel={model}
//           onModelRegistered={(name) => { setModel(name); setTab('chat'); }}
//           onRefreshModels={refreshModels}
//         />
//       )}

//       <footer style={{textAlign:'center', color:'#9aa3b2', fontSize:12}}>
//         Frontend talks to ASP.NET API at <code>{import.meta.env.VITE_API_BASE || 'http://localhost:5000'}</code>
//       </footer>
//     </div>
//   )
// }

// 1st ----------------------------------------------------------------------------

// import React, { useEffect, useState } from 'react'
// import ChatWindow from './components/ChatWindow'
// import { listModels } from './lib/api'

// export default function App() {
//   const [models, setModels] = useState<{name: string}[]>([])
//   const [model, setModel] = useState('')

//   useEffect(() => {
//     (async () => {
//       const list = await listModels()
//       setModels(list)
//       if (!model && list.length) setModel(list[0].name)
//     })()
//     // eslint-disable-next-line react-hooks/exhaustive-deps
//   }, [])

//   return (
//     <div className="app">
//       <header className="header">
//         <h1>LocalLLM — Chat</h1>
//         <span className="spacer" />
//         <label>
//           <span style={{marginRight:8}}>Model</span>
//           <select className="select" value={model} onChange={e => setModel(e.target.value)}>
//             {models.map(m => <option key={m.name} value={m.name}>{m.name}</option>)}
//           </select>
//         </label>
//         <span className="model-note">Tip: register your latest run via /api/model/register.</span>
//       </header>
//       <ChatWindow model={model} />
//       <footer style={{textAlign:'center', color:'#9aa3b2', fontSize:12}}>
//         Frontend talks to ASP.NET API at <code>{import.meta.env.VITE_API_BASE || 'http://localhost:5000'}</code>
//       </footer>
//     </div>
//   )
// }

import React, { useEffect, useMemo, useRef, useState } from 'react'
import { streamChat, type ChatRequest } from '../lib/api'

type Message = { role: 'user' | 'assistant', text: string }

export default function ChatWindow({ model }: { model: string }) {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const abortRef = useRef<AbortController | null>(null)
  const scroller = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    scroller.current?.lastElementChild?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const placeholder = useMemo(() => model ? `Send a message to ${model}…` : 'Send a message…', [model])

  async function onSend() {
    if (!input.trim() || busy) return
    const prompt = input.trim()
    setInput('')
    setMessages(m => [...m, { role: 'user', text: prompt }, { role: 'assistant', text: '' }])
    setBusy(true)
    const idx = messages.length + 1

    abortRef.current?.abort()
    const ac = new AbortController()
    abortRef.current = ac

    try {
      const req: ChatRequest = { Prompt: buildPromptWithHistory([...messages, { role: 'user', text: prompt }]), Model: model || undefined }
      for await (const chunk of streamChat(req, ac.signal)) {
        setMessages(m => {
          const copy = m.slice()
          copy[idx] = { role: 'assistant', text: (copy[idx]?.text ?? '') + chunk }
          return copy
        })
      }
    } catch (err: any) {
      setMessages(m => [...m, { role: 'assistant', text: `❌ ${err?.message || String(err)}` }])
    } finally {
      setBusy(false)
    }
  }

  function buildPromptWithHistory(history: Message[]): string {
    const lines: string[] = []
    for (const msg of history) {
      if (msg.role === 'user') lines.push(`User: ${msg.text}`)
      else lines.push(`Assistant: ${msg.text}`)
    }
    lines.push('Assistant:')
    return lines.join('\n')
  }

  return (
    <div className="main">
      <div className="chat" ref={scroller}>
        {messages.length === 0 && (
          <div className="msg-row">
            <div className="msg assistant" style={{opacity:.95}}>
              <div className="label">Assistant</div>
              👋 Welcome! Ask me anything about your FAQ dataset or try “What is our return policy?”.
            </div>
          </div>
        )}
        {messages.map((m, i) => (
          <div key={i} className={"msg-row " + (m.role === 'user' ? 'user' : 'assistant')}>
            <div className={"msg " + (m.role === 'user' ? 'user' : 'assistant')}>
              <div className="label">{m.role === 'user' ? 'You' : 'Assistant'}</div>
              {m.text || (m.role === 'assistant' ? '▍' : '')}
            </div>
          </div>
        ))}
      </div>

      <div className="composer">
        <textarea
          className="textarea"
          placeholder={placeholder}
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); onSend(); } }}
        />
        <button className="button" onClick={onSend} disabled={busy || !input.trim()}>
          {busy ? 'Generating…' : 'Send'}
        </button>
        <button className="button" onClick={() => abortRef.current?.abort()} disabled={!busy}>
          Stop
        </button>
      </div>
    </div>
  )
}


// 1st ----------------------------------------------------------------------------------------------
// import React, { useEffect, useMemo, useRef, useState } from 'react'
// import { streamChat, type ChatRequest } from '../lib/api'

// type Message = { role: 'user' | 'assistant', text: string }

// export default function ChatWindow({ model }: { model: string }) {
//   const [messages, setMessages] = useState<Message[]>([])
//   const [input, setInput] = useState('')
//   const [busy, setBusy] = useState(false)
//   const abortRef = useRef<AbortController | null>(null)
//   const scroller = useRef<HTMLDivElement | null>(null)

//   useEffect(() => {
//     scroller.current?.lastElementChild?.scrollIntoView({ behavior: 'smooth' })
//   }, [messages])

//   const placeholder = useMemo(() => model ? `Send a message to ${model}…` : 'Send a message…', [model])

//   async function onSend() {
//     if (!input.trim() || busy) return
//     const prompt = input
//     setInput('')
//     setMessages(m => [...m, { role: 'user', text: prompt }, { role: 'assistant', text: '' }])
//     setBusy(true)
//     const idx = messages.length + 1 // where the assistant message sits

//     abortRef.current?.abort()
//     const ac = new AbortController()
//     abortRef.current = ac

//     try {
//       const req: ChatRequest = { Prompt: buildPromptWithHistory([...messages, { role: 'user', text: prompt }]), Model: model || undefined }
//       for await (const chunk of streamChat(req, ac.signal)) {
//         setMessages(m => {
//           const copy = m.slice()
//           copy[idx] = { role: 'assistant', text: (copy[idx]?.text ?? '') + chunk }
//           return copy
//         })
//       }
//     } catch (err: any) {
//       setMessages(m => [...m, { role: 'assistant', text: `❌ ${err?.message || String(err)}` }])
//     } finally {
//       setBusy(false)
//     }
//   }

//   function buildPromptWithHistory(history: Message[]): string {
//     // Compact in-context format since the backend streams plain text
//     const lines: string[] = []
//     for (const msg of history) {
//       if (msg.role === 'user') lines.push(`User: ${msg.text}`)
//       else lines.push(`Assistant: ${msg.text}`)
//     }
//     lines.push('Assistant:')
//     return lines.join('\n')
//   }

//   return (
//     <div className="main">
//       <div className="chat" ref={scroller}>
//         {messages.length === 0 && (
//           <div className="msg assistant" style={{opacity:.8}}>👋 Ask me anything about your FAQ data.</div>
//         )}
//         {messages.map((m, i) => (
//           <div key={i} className={"msg " + (m.role === 'user' ? 'user' : 'assistant')}>
//             {m.text || (m.role === 'assistant' ? '▍' : '')}
//           </div>
//         ))}
//       </div>
//       <div className="footer">
//         <textarea
//           className="input"
//           placeholder={placeholder}
//           value={input}
//           onChange={e => setInput(e.target.value)}
//           onKeyDown={e => {
//             if (e.key === 'Enter' && !e.shiftKey) {
//               e.preventDefault()
//               onSend()
//             }
//           }}
//         />
//         <button className="button" onClick={onSend} disabled={busy || !input.trim()}>
//           {busy ? 'Generating…' : 'Send'}
//         </button>
//         <button className="button" onClick={() => abortRef.current?.abort()} disabled={!busy}>
//           Stop
//         </button>
//       </div>
//     </div>
//   )
// }

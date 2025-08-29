// This file contains the ChatWindow component.
import React, { useState, useRef, useEffect } from 'react'
import { chat } from '../lib/api'

interface Props {
  model: string
}
type Message = { role: 'user' | 'assistant', content: string }
/// <summary>
/// The ChatWindow component.
/// </summary>
export default function ChatWindow({ model }: Props) {
  const [prompt, setPrompt] = useState('')
  const [messages, setMessages] = useState<Message[]>([])
  const [isStreaming, setIsStreaming] = useState(false)
  const abortController = useRef<AbortController | null>(null)
  const chatContainer = useRef<HTMLDivElement>(null)

  useEffect(() => {
    // Scroll to bottom when new messages are added
    if (chatContainer.current) {
      chatContainer.current.scrollTop = chatContainer.current.scrollHeight
    }
  }, [messages])
  /// <summary>
  /// Handles sending a chat message.
  /// </summary>
  async function handleSend() {
    if (!prompt.trim() || !model) return
    setIsStreaming(true)
    abortController.current = new AbortController()

    const newMessages: Message[] = [...messages, { role: 'user', content: prompt }]
    setMessages(newMessages)
    setPrompt('')

    try {
      const stream = chat(prompt, model, abortController.current.signal)
      let fullResponse = ''
      for await (const chunk of stream) {
        fullResponse += chunk
        setMessages([...newMessages, { role: 'assistant', content: fullResponse }])
      }
    } catch (err: any) {
      if (err.name !== 'AbortError') {
        console.error(err)
        setMessages([...newMessages, { role: 'assistant', content: `Error: ${err.message}` }])
      }
    }
    setIsStreaming(false)
  }
  /// <summary>
  /// Handles stopping the chat stream.
  /// </summary>
  function handleStop() {
    if (abortController.current) {
      abortController.current.abort()
    }
  }

  return (
    <div className="main">
      <div className="chat" ref={chatContainer}>
        {messages.map((msg, i) => (
          <div key={i} className={`msg-row ${msg.role}`}>
            <div className={`msg ${msg.role}`}>
              <div className="label">{msg.role}</div>
              {msg.content}
            </div>
          </div>
        ))}
      </div>

      <div className="composer">
        <textarea
          className="textarea"
          value={prompt}
          onChange={e => setPrompt(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); } }}
          placeholder="Ask me anything..."
        />
        <button className="button" onClick={handleSend} disabled={isStreaming || !prompt.trim() || !model}>
          Send
        </button>
        {isStreaming && <button className="button" onClick={handleStop}>Stop</button>}
      </div>
    </div>
  )
}
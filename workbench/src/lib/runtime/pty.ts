import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus, TaskRequest, TaskResult } from '@/types'

/**
 * PtyRuntime - Interactive Claude Code CLI sessions via HTTP/SSE.
 *
 * Cross-platform architecture:
 * - Output: SSE stream from /api/pty/{sessionId}/stream
 * - Input: HTTP POST to /api/pty/{sessionId}/input (or Photino IPC as fallback)
 *
 * Primary use: Multi-turn conversations requiring user interaction
 * - Spec elicitation (chat to build spec)
 * - Work loop with input prompts
 * - Debug sessions
 */

// Session state
interface Session {
  id: string
  status: RunStatus
  eventSource: EventSource | null
  handlers: Set<(event: RunEvent) => void>
  outputHandlers: Set<(data: string) => void>
  exitHandlers: Set<(exitCode: number) => void>
  // Buffer for events that arrive before handlers are registered
  outputBuffer: string[]
}

// Shared session state (module-level for persistence)
const sessions = new Map<string, Session>()

// Photino IPC for input (works on all platforms)
function getPhotinoExternal(): { sendMessage: (msg: string) => void } | null {
  if (typeof window === 'undefined') return null
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const ext = (window as any).external
  if (ext && typeof ext.sendMessage === 'function') {
    return ext
  }
  return null
}

export function createPtyRuntime(): Runtime {
  return {
    async start(issue: number): Promise<RunHandle> {
      // Start session via HTTP
      const response = await fetch('/api/pty/start', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          command: 'claude',
          args: ['--issue', issue.toString()],
          cols: 80,
          rows: 24,
        }),
      })

      if (!response.ok) {
        throw new Error(`Failed to start PTY session: ${response.statusText}`)
      }

      const { sessionId } = await response.json()

      const session: Session = {
        id: sessionId,
        status: 'running',
        eventSource: null,
        handlers: new Set(),
        outputHandlers: new Set(),
        exitHandlers: new Set(),
        outputBuffer: [],
      }
      sessions.set(sessionId, session)

      // Connect SSE
      connectSSE(session)

      return {
        id: sessionId,
        issue,
        status: session.status,
      }
    },

    async send(runId: string, input: string): Promise<void> {
      const session = sessions.get(runId)
      if (!session) {
        throw new Error(`Session ${runId} not found`)
      }

      // Try Photino IPC first (lower latency), fall back to HTTP
      const photino = getPhotinoExternal()
      if (photino) {
        photino.sendMessage(
          JSON.stringify({
            type: 'pty_input',
            sessionId: runId,
            data: input,
          })
        )
      } else {
        await fetch(`/api/pty/${runId}/input`, {
          method: 'POST',
          body: input,
        })
      }
    },

    async abort(runId: string): Promise<void> {
      const session = sessions.get(runId)
      if (!session) return

      session.eventSource?.close()
      await fetch(`/api/pty/${runId}`, { method: 'DELETE' })

      session.status = 'aborted'
      session.handlers.forEach((h) => h({ type: 'status', runId, status: 'aborted' }))
      sessions.delete(runId)
    },

    subscribe(runId: string, handler: (event: RunEvent) => void): () => void {
      let session = sessions.get(runId)
      if (!session) {
        session = {
          id: runId,
          status: 'starting',
          eventSource: null,
          handlers: new Set(),
          outputHandlers: new Set(),
          exitHandlers: new Set(),
          outputBuffer: [],
        }
        sessions.set(runId, session)
      }

      session.handlers.add(handler)
      return () => {
        session?.handlers.delete(handler)
      }
    },

    status(runId: string): RunStatus | null {
      return sessions.get(runId)?.status ?? null
    },

    async execute(_task: TaskRequest): Promise<TaskResult> {
      throw new Error(
        'PtyRuntime does not support non-interactive tasks. Use ApiRuntime.execute() instead.'
      )
    },
  }
}

function connectSSE(session: Session) {
  const eventSource = new EventSource(`/api/pty/${session.id}/stream`)
  session.eventSource = eventSource

  eventSource.onmessage = (event) => {
    try {
      const data = JSON.parse(event.data)

      switch (data.type) {
        case 'output':
          // Buffer output if no handlers registered yet
          if (session.outputHandlers.size === 0) {
            session.outputBuffer.push(data.data)
          } else {
            session.outputHandlers.forEach((h) => h(data.data))
          }
          session.handlers.forEach((h) =>
            h({ type: 'output', runId: session.id, text: data.data })
          )
          break

        case 'exit':
          session.status = data.exitCode === 0 ? 'done' : 'failed'
          session.exitHandlers.forEach((h) => h(data.exitCode))
          session.handlers.forEach((h) =>
            h({ type: 'status', runId: session.id, status: session.status })
          )
          eventSource.close()
          break

        case 'error':
          session.status = 'failed'
          session.handlers.forEach((h) =>
            h({ type: 'error', runId: session.id, message: data.error })
          )
          eventSource.close()
          break
      }
    } catch {
      // Ignore parse errors
    }
  }

  eventSource.onerror = () => {
    // SSE connection lost - session may have ended
    if (session.status === 'running') {
      session.status = 'failed'
      session.handlers.forEach((h) =>
        h({ type: 'error', runId: session.id, message: 'Connection lost' })
      )
    }
    eventSource.close()
  }
}

// === Interactive session API for Terminal component ===

export interface InteractiveSession {
  sessionId: string
  onOutput: (handler: (data: string) => void) => () => void
  onExit: (handler: (exitCode: number) => void) => () => void
  write: (data: string) => void
  resize: (cols: number, rows: number) => void
  kill: () => void
}

/**
 * Start an interactive PTY session with Claude Code.
 * Used by Terminal component in Shape workspace.
 */
export async function startInteractiveSession(options: {
  command: string
  args?: string[]
  cwd?: string
  cols?: number
  rows?: number
}): Promise<InteractiveSession> {
  // Start session via HTTP
  const response = await fetch('/api/pty/start', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      command: options.command,
      args: options.args ?? [],
      cwd: options.cwd,
      cols: options.cols ?? 80,
      rows: options.rows ?? 24,
    }),
  })

  if (!response.ok) {
    const error = await response.text()
    throw new Error(`Failed to start PTY session: ${error}`)
  }

  const { sessionId } = await response.json()

  const session: Session = {
    id: sessionId,
    status: 'running',
    eventSource: null,
    handlers: new Set(),
    outputHandlers: new Set(),
    exitHandlers: new Set(),
    outputBuffer: [],
  }
  sessions.set(sessionId, session)

  // Connect SSE for output
  connectSSE(session)

  return {
    sessionId,

    onOutput(handler) {
      session.outputHandlers.add(handler)
      // Flush any buffered output that arrived before handler was registered
      // Use requestAnimationFrame to let the terminal fully initialize before flushing
      if (session.outputBuffer.length > 0) {
        const buffered = session.outputBuffer.join('')
        session.outputBuffer = []
        requestAnimationFrame(() => handler(buffered))
      }
      return () => session.outputHandlers.delete(handler)
    },

    onExit(handler) {
      session.exitHandlers.add(handler)
      return () => session.exitHandlers.delete(handler)
    },

    write(data) {
      // Try Photino IPC first (lower latency), fall back to HTTP
      const photino = getPhotinoExternal()
      if (photino) {
        photino.sendMessage(
          JSON.stringify({
            type: 'pty_input',
            sessionId,
            data,
          })
        )
      } else {
        fetch(`/api/pty/${sessionId}/input`, {
          method: 'POST',
          body: data,
        })
      }
    },

    resize(cols, rows) {
      // Try Photino IPC first, fall back to HTTP
      const photino = getPhotinoExternal()
      if (photino) {
        photino.sendMessage(
          JSON.stringify({
            type: 'pty_resize',
            sessionId,
            cols,
            rows,
          })
        )
      } else {
        fetch(`/api/pty/${sessionId}/resize`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ cols, rows }),
        })
      }
    },

    kill() {
      session.eventSource?.close()
      fetch(`/api/pty/${sessionId}`, { method: 'DELETE' })
      sessions.delete(sessionId)
    },
  }
}

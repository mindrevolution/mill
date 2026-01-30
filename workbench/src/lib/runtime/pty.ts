import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus, TaskRequest, TaskResult } from '@/types'

/**
 * PtyRuntime - Interactive Claude Code CLI sessions via Photino + Pty.Net.
 *
 * Primary use: Multi-turn conversations requiring user interaction
 * - Spec elicitation (chat to build spec)
 * - Work loop with input prompts
 * - Debug sessions
 *
 * Architecture:
 * - Pty.Net spawns Claude Code with full PTY support (.NET side)
 * - xterm.js renders terminal output (React side)
 * - Photino IPC bridges the two (no WebSocket needed)
 *
 * Non-interactive tasks (execute) should use ApiRuntime instead.
 *
 * Implementation requires:
 * - Photino.NET for webview hosting
 * - Pty.Net (microsoft/vs-pty.net) for cross-platform PTY
 * - xterm.js (@xterm/xterm) for terminal rendering
 * - Photino IPC message handlers
 */
export function createPtyRuntime(): Runtime {
  // Photino IPC will be available as window.external when running in Photino
  // const photino = (window as any).external

  const runs = new Map<string, { status: RunStatus; handlers: Set<(event: RunEvent) => void> }>()

  return {
    // === Interactive methods ===

    async start(issue: number): Promise<RunHandle> {
      // TODO: Send message to .NET to spawn PTY
      // photino.sendMessage(JSON.stringify({ type: 'start_run', issue }))
      throw new Error(
        `PtyRuntime.start(${issue}): Not implemented. Requires Photino + Pty.Net setup.`
      )
    },

    async send(runId: string, _input: string): Promise<void> {
      // TODO: Send input to PTY via Photino IPC
      // photino.sendMessage(JSON.stringify({ type: 'pty_input', runId, input }))
      throw new Error(
        `PtyRuntime.send(${runId}): Not implemented. Requires Photino + Pty.Net setup.`
      )
    },

    async abort(runId: string): Promise<void> {
      // TODO: Send abort signal to PTY
      // photino.sendMessage(JSON.stringify({ type: 'abort_run', runId }))
      throw new Error(
        `PtyRuntime.abort(${runId}): Not implemented. Requires Photino + Pty.Net setup.`
      )
    },

    subscribe(runId: string, handler: (event: RunEvent) => void): () => void {
      // TODO: Register handler for Photino messages
      // photino.receiveMessage((msg) => { ... })
      let run = runs.get(runId)
      if (!run) {
        run = { status: 'starting', handlers: new Set() }
        runs.set(runId, run)
      }
      run.handlers.add(handler)
      return () => {
        run?.handlers.delete(handler)
      }
    },

    status(runId: string): RunStatus | null {
      return runs.get(runId)?.status ?? null
    },

    // === Non-interactive: Not supported ===

    async execute(_task: TaskRequest): Promise<TaskResult> {
      throw new Error(
        'PtyRuntime does not support non-interactive tasks. Use ApiRuntime.execute() instead.'
      )
    },
  }
}

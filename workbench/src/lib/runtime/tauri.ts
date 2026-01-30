import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus, TaskRequest, TaskResult } from '@/types'

/**
 * TauriRuntime - Interactive Claude Code CLI sessions via embedded PTY.
 *
 * Primary use: Multi-turn conversations requiring user interaction
 * - Spec elicitation (chat to build spec)
 * - Work loop with input prompts
 * - Debug sessions
 *
 * Runs Claude Code in an embedded terminal within the Tauri app.
 * User sees output in real-time, can interrupt, provide input.
 *
 * Non-interactive tasks (execute) should use ApiRuntime instead.
 *
 * Implementation requires src-tauri/ setup with:
 * - Rust commands: start_run, send_input, abort_run
 * - PTY handling for terminal emulation
 * - Tauri event system for streaming output
 */
export function createTauriRuntime(): Runtime {
  // Will be populated when Tauri is integrated:
  // const { invoke } = await import('@tauri-apps/api/core')
  // const { listen } = await import('@tauri-apps/api/event')

  const runs = new Map<string, { status: RunStatus; handlers: Set<(event: RunEvent) => void> }>()

  return {
    // === Interactive methods ===

    async start(issue: number): Promise<RunHandle> {
      // TODO: invoke('start_run', { issue })
      // Returns run ID, spawns Claude Code with PTY
      // Events streamed via Tauri event system
      throw new Error(
        `TauriRuntime.start(${issue}): Not implemented. Requires src-tauri/ setup.`
      )
    },

    async send(runId: string, _input: string): Promise<void> {
      // TODO: invoke('send_input', { runId, input })
      // Writes to PTY stdin
      throw new Error(
        `TauriRuntime.send(${runId}): Not implemented. Requires src-tauri/ setup.`
      )
    },

    async abort(runId: string): Promise<void> {
      // TODO: invoke('abort_run', { runId })
      // Sends SIGINT to process
      throw new Error(
        `TauriRuntime.abort(${runId}): Not implemented. Requires src-tauri/ setup.`
      )
    },

    subscribe(runId: string, handler: (event: RunEvent) => void): () => void {
      // TODO: listen(`run:${runId}`, handler)
      // Subscribes to Tauri events for this run
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
        'TauriRuntime does not support non-interactive tasks. Use ApiRuntime.execute() instead.'
      )
    },
  }
}

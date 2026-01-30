import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus } from '@/types'

/**
 * TauriRuntime - Stub for Tauri desktop integration
 *
 * When src-tauri/ is added, this will:
 * - Call Rust commands via @tauri-apps/api: start_run, send_input, abort_run
 * - Subscribe to Tauri events for run updates
 * - Handle process spawning for the mill CLI
 */
export function createTauriRuntime(): Runtime {
  // This will be populated when Tauri is integrated
  // const { invoke } = await import('@tauri-apps/api/core')
  // const { listen } = await import('@tauri-apps/api/event')

  const runs = new Map<string, { status: RunStatus; handlers: Set<(event: RunEvent) => void> }>()

  return {
    async start(_issue: number): Promise<RunHandle> {
      // TODO: invoke('start_run', { issue })
      throw new Error('TauriRuntime not implemented. Waiting for src-tauri/ setup.')
    },

    async send(_runId: string, _input: string): Promise<void> {
      // TODO: invoke('send_input', { runId, input })
      throw new Error('TauriRuntime not implemented. Waiting for src-tauri/ setup.')
    },

    async abort(_runId: string): Promise<void> {
      // TODO: invoke('abort_run', { runId })
      throw new Error('TauriRuntime not implemented. Waiting for src-tauri/ setup.')
    },

    subscribe(runId: string, handler: (event: RunEvent) => void): () => void {
      // TODO: listen(`run:${runId}`, handler)
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
  }
}

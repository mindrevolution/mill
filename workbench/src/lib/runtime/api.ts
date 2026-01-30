import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus } from '@/types'

/**
 * ApiRuntime - Stub for hosted web deployment
 *
 * When the API backend supports WebSocket streaming, this will:
 * - POST /api/run/{issue}/start to initiate a run
 * - POST /api/run/{id}/input to send input
 * - DELETE /api/run/{id} to abort
 * - WebSocket at /api/run/{id}/stream for events
 */
export function createApiRuntime(_baseUrl?: string): Runtime {
  const runs = new Map<string, { status: RunStatus; handlers: Set<(event: RunEvent) => void> }>()

  return {
    async start(_issue: number): Promise<RunHandle> {
      // TODO: POST to /api/run/{issue}/start
      // TODO: Connect WebSocket to /api/run/{id}/stream
      throw new Error('ApiRuntime not implemented. Waiting for backend WebSocket support.')
    },

    async send(_runId: string, _input: string): Promise<void> {
      // TODO: POST to /api/run/{runId}/input
      throw new Error('ApiRuntime not implemented. Waiting for backend WebSocket support.')
    },

    async abort(_runId: string): Promise<void> {
      // TODO: DELETE /api/run/{runId}
      throw new Error('ApiRuntime not implemented. Waiting for backend WebSocket support.')
    },

    subscribe(runId: string, handler: (event: RunEvent) => void): () => void {
      // TODO: WebSocket subscription
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

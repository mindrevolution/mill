import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus, TaskRequest, TaskResult } from '@/types'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5218'

/**
 * ApiRuntime - Non-interactive Claude Code CLI execution via API.
 *
 * Primary use: Single prompt → structured response tasks
 * - Add observation to library
 * - Context warmup
 * - Criterion verification
 * - Spec refinement
 *
 * The API server spawns: claude --print "prompt" --output-format json
 * and returns the parsed result.
 *
 * Interactive methods (start/send/abort) throw - use PtyRuntime for those.
 *
 * Future (hosted): Can swap CLI spawning for direct Anthropic API calls.
 */
export function createApiRuntime(baseUrl = API_BASE): Runtime {
  return {
    // === Interactive methods: Not supported ===

    async start(_issue: number): Promise<RunHandle> {
      throw new Error(
        'ApiRuntime does not support interactive runs. Use PtyRuntime for interactive sessions.'
      )
    },

    async send(_runId: string, _input: string): Promise<void> {
      throw new Error(
        'ApiRuntime does not support interactive runs. Use PtyRuntime for interactive sessions.'
      )
    },

    async abort(_runId: string): Promise<void> {
      throw new Error(
        'ApiRuntime does not support interactive runs. Use PtyRuntime for interactive sessions.'
      )
    },

    subscribe(_runId: string, _handler: (event: RunEvent) => void): () => void {
      console.warn('ApiRuntime.subscribe: No-op, interactive runs not supported')
      return () => {}
    },

    status(_runId: string): RunStatus | null {
      return null
    },

    // === Non-interactive task execution ===

    async execute(task: TaskRequest): Promise<TaskResult> {
      const res = await fetch(`${baseUrl}/api/task/execute`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(task),
      })

      if (!res.ok) {
        const text = await res.text()
        return {
          success: false,
          output: '',
          error: `API error: ${res.status} ${res.statusText} - ${text}`,
        }
      }

      const result = await res.json() as TaskResult
      return result
    },
  }
}

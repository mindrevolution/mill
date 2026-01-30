import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunHandle, RunStatus, TaskRequest, TaskResult } from '@/types'

interface MockRun {
  id: string
  issue: number
  status: RunStatus
  iteration: number
  maxIterations: number
  handlers: Set<(event: RunEvent) => void>
  timer: ReturnType<typeof setInterval> | null
}

const mockOutputs = [
  '• reading spec from issue...',
  '✓ spec loaded',
  '• analyzing requirements...',
  '  ↳ found 3 acceptance criteria',
  '• implementing changes...',
  '  ↳ created new component',
  '  ↳ updated styles',
  '• running tests...',
  '✓ tests passed',
  '• verifying against criteria...',
]

const mockTools = [
  { tool: 'Read', args: 'src/components/feature.tsx' },
  { tool: 'Glob', args: '**/*.tsx' },
  { tool: 'Edit', args: 'src/components/feature.tsx' },
  { tool: 'Bash', args: 'pnpm test' },
]

export function createMockRuntime(options: { delay?: number } = {}): Runtime {
  const { delay = 1500 } = options
  const runs = new Map<string, MockRun>()
  let nextId = 1

  function emit(run: MockRun, event: RunEvent) {
    run.handlers.forEach((handler) => handler(event))
  }

  function simulateRun(run: MockRun) {
    let step = 0
    const totalSteps = mockOutputs.length + mockTools.length

    run.timer = setInterval(() => {
      if (run.status === 'aborted') {
        if (run.timer) clearInterval(run.timer)
        return
      }

      step++

      // Emit tool calls interleaved with output
      if (step % 3 === 0 && mockTools.length > 0) {
        const toolIndex = Math.floor(step / 3) % mockTools.length
        const tool = mockTools[toolIndex]
        emit(run, { type: 'tool_call', runId: run.id, tool: tool.tool, args: tool.args })

        setTimeout(() => {
          emit(run, { type: 'tool_result', runId: run.id, tool: tool.tool, result: 'ok' })
        }, delay / 2)
      }

      // Emit output
      const outputIndex = step % mockOutputs.length
      emit(run, { type: 'output', runId: run.id, text: mockOutputs[outputIndex] })

      // Update iteration periodically
      if (step % 4 === 0) {
        run.iteration = Math.min(run.iteration + 1, run.maxIterations)
        emit(run, { type: 'iteration', runId: run.id, current: run.iteration, max: run.maxIterations })
      }

      // Transition to verifying after some steps
      if (step === 8 && run.status === 'running') {
        run.status = 'verifying'
        emit(run, { type: 'status', runId: run.id, status: 'verifying' })
      }

      // Complete after more steps
      if (step >= totalSteps) {
        run.status = 'done'
        emit(run, { type: 'status', runId: run.id, status: 'done' })
        emit(run, { type: 'output', runId: run.id, text: '✓ all criteria verified' })
        if (run.timer) clearInterval(run.timer)
      }
    }, delay)
  }

  return {
    async start(issue: number): Promise<RunHandle> {
      const id = `mock-${nextId++}`
      const run: MockRun = {
        id,
        issue,
        status: 'starting',
        iteration: 1,
        maxIterations: 5,
        handlers: new Set(),
        timer: null,
      }

      runs.set(id, run)

      // Emit starting status, then transition to running
      setTimeout(() => {
        run.status = 'running'
        emit(run, { type: 'status', runId: id, status: 'running' })
        emit(run, { type: 'iteration', runId: id, current: 1, max: 5 })
        simulateRun(run)
      }, 500)

      return { id, issue, status: 'starting' }
    },

    async send(runId: string, input: string): Promise<void> {
      const run = runs.get(runId)
      if (!run) {
        console.warn(`MockRuntime: no run with id ${runId}`)
        return
      }

      console.log(`MockRuntime: received input for ${runId}:`, input)
      emit(run, { type: 'output', runId, text: `> ${input}` })
    },

    async abort(runId: string): Promise<void> {
      const run = runs.get(runId)
      if (!run) return

      if (run.timer) {
        clearInterval(run.timer)
        run.timer = null
      }

      run.status = 'aborted'
      emit(run, { type: 'status', runId, status: 'aborted' })
      emit(run, { type: 'output', runId, text: '• run aborted by user' })
    },

    subscribe(runId: string, handler: (event: RunEvent) => void): () => void {
      const run = runs.get(runId)
      if (!run) {
        console.warn(`MockRuntime: subscribing to unknown run ${runId}`)
        return () => {}
      }

      run.handlers.add(handler)
      return () => {
        run.handlers.delete(handler)
      }
    },

    status(runId: string): RunStatus | null {
      const run = runs.get(runId)
      return run?.status ?? null
    },

    // === Non-interactive task execution (mock) ===

    async execute(task: TaskRequest): Promise<TaskResult> {
      // Simulate API delay
      await new Promise((resolve) => setTimeout(resolve, delay))

      // Mock responses by task type
      const mockResponses: Record<string, TaskResult> = {
        'add-observation': {
          success: true,
          output: 'Created library entry: .mill/standards/new-standard.md',
          data: { file: '.mill/standards/new-standard.md' },
        },
        'context-warmup': {
          success: true,
          output: 'Generated context: .mill/context.md (2.3kb)',
          data: { file: '.mill/context.md', size: 2300 },
        },
        'verify-criterion': {
          success: true,
          output: 'Criterion verified: All acceptance criteria pass',
          data: { passed: true, criterion: task.context?.criterion },
        },
        'refine-spec': {
          success: true,
          output: 'Spec updated with current codebase context',
          data: { updated: true },
        },
      }

      const result = mockResponses[task.type]
      if (result) {
        return result
      }

      return {
        success: false,
        output: '',
        error: `Unknown task type: ${task.type}`,
      }
    },
  }
}

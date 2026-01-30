import { useState, useEffect, useCallback, useMemo } from 'react'
import { getRuntime, setRuntime, detectRuntime } from '@/lib/runtime'
import { createMockRuntime, createTauriRuntime, createApiRuntime } from '@/lib/runtime/index'
import type { Runtime } from '@/lib/runtime'
import type { RunEvent, RunStatus } from '@/types'

let initialized = false

function initializeRuntime(): Runtime {
  if (initialized) {
    return getRuntime()
  }

  const runtimeType = detectRuntime()
  let runtime: Runtime

  switch (runtimeType) {
    case 'tauri':
      runtime = createTauriRuntime()
      break
    case 'api':
      // ApiRuntime will throw if used before backend is ready
      runtime = createApiRuntime()
      break
    case 'mock':
    default:
      runtime = createMockRuntime()
  }

  setRuntime(runtime)
  initialized = true
  return runtime
}

/**
 * Hook to get the runtime instance
 */
export function useRuntime(): Runtime {
  return useMemo(() => initializeRuntime(), [])
}

/**
 * Hook to track a specific run's state and events
 */
export function useRun(runId: string | null): {
  status: RunStatus | null
  events: RunEvent[]
  send: (input: string) => void
  abort: () => void
} {
  const runtime = useRuntime()
  const [status, setStatus] = useState<RunStatus | null>(null)
  const [events, setEvents] = useState<RunEvent[]>([])

  useEffect(() => {
    if (!runId) {
      setStatus(null)
      setEvents([])
      return
    }

    // Get initial status
    setStatus(runtime.status(runId))

    // Subscribe to events
    const unsubscribe = runtime.subscribe(runId, (event) => {
      setEvents((prev) => [...prev, event])

      if (event.type === 'status') {
        setStatus(event.status)
      }
    })

    return () => {
      unsubscribe()
    }
  }, [runId, runtime])

  const send = useCallback(
    (input: string) => {
      if (runId) {
        runtime.send(runId, input)
      }
    },
    [runId, runtime]
  )

  const abort = useCallback(() => {
    if (runId) {
      runtime.abort(runId)
    }
  }, [runId, runtime])

  return { status, events, send, abort }
}

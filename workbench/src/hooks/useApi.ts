import { useState, useEffect, useCallback, useRef } from 'react'
import { api, createDraftEventSource } from '@/lib/api'

interface UseQueryResult<T> {
  data: T | undefined
  loading: boolean
  error: Error | undefined
  refetch: (forceRefresh?: boolean) => void
}

function useQuery<T>(
  fetcher: (refresh: boolean) => Promise<T>,
  deps: unknown[] = []
): UseQueryResult<T> {
  const [data, setData] = useState<T | undefined>()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<Error | undefined>()

  const fetch = useCallback(async (forceRefresh = false) => {
    setLoading(true)
    setError(undefined)
    try {
      const result = await fetcher(forceRefresh)
      setData(result)
    } catch (e) {
      setError(e instanceof Error ? e : new Error(String(e)))
    } finally {
      setLoading(false)
    }
  }, deps)

  useEffect(() => {
    fetch(false)
  }, [fetch])

  return { data, loading, error, refetch: fetch }
}

// Spec hooks
export function useDrafts() {
  return useQuery(() => api.spec.drafts(), [])
}

export function useIssues() {
  return useQuery((refresh) => api.spec.issues(refresh), [])
}

export function useSpecs() {
  const drafts = useDrafts()
  const issues = useIssues()
  const eventSourceRef = useRef<EventSource | null>(null)

  // Subscribe to draft file change events
  useEffect(() => {
    const es = createDraftEventSource(() => {
      // Refetch drafts when files change
      drafts.refetch()
    })
    eventSourceRef.current = es

    return () => {
      es.close()
      eventSourceRef.current = null
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  return {
    drafts: drafts.data ?? [],
    issues: issues.data ?? [],
    loading: drafts.loading || issues.loading,
    error: drafts.error || issues.error,
    refetch: (forceRefresh = false) => {
      drafts.refetch()
      issues.refetch(forceRefresh)
    },
  }
}

// Knowledge hooks
export function useKnowledge(category: string) {
  return useQuery(() => api.knowledge.list(category), [category])
}

// Runs hooks
export function useRuns() {
  return useQuery(() => api.run.list(), [])
}

// History hooks
export function useHistory() {
  return useQuery(() => api.history(), [])
}

// Brief hooks
export function useBriefs() {
  return useQuery(() => api.brief.list(), [])
}

export function useDroppedBriefs() {
  return useQuery(() => api.brief.dropped(), [])
}

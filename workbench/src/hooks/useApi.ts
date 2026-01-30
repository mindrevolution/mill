import { useState, useEffect, useCallback } from 'react'
import { api } from '@/lib/api'

interface UseQueryResult<T> {
  data: T | undefined
  loading: boolean
  error: Error | undefined
  refetch: () => void
}

function useQuery<T>(fetcher: () => Promise<T>, deps: unknown[] = []): UseQueryResult<T> {
  const [data, setData] = useState<T | undefined>()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<Error | undefined>()

  const fetch = useCallback(async () => {
    setLoading(true)
    setError(undefined)
    try {
      const result = await fetcher()
      setData(result)
    } catch (e) {
      setError(e instanceof Error ? e : new Error(String(e)))
    } finally {
      setLoading(false)
    }
  }, deps)

  useEffect(() => {
    fetch()
  }, [fetch])

  return { data, loading, error, refetch: fetch }
}

// Spec hooks
export function useDrafts() {
  return useQuery(() => api.spec.drafts(), [])
}

export function useIssues() {
  return useQuery(() => api.spec.issues(), [])
}

export function useSpecs() {
  const drafts = useDrafts()
  const issues = useIssues()

  return {
    drafts: drafts.data ?? [],
    issues: issues.data ?? [],
    loading: drafts.loading || issues.loading,
    error: drafts.error || issues.error,
    refetch: () => {
      drafts.refetch()
      issues.refetch()
    },
  }
}

// Library hooks
export function useLibrary(category: string) {
  return useQuery(() => api.library.list(category), [category])
}

// Runs hooks
export function useRuns() {
  return useQuery(() => api.run.list(), [])
}

// History hooks
export function useHistory() {
  return useQuery(() => api.history(), [])
}

import { create } from 'zustand'
import { useShallow } from 'zustand/react/shallow'
import { api, createJobEventSource, type Job, type CreateJobRequest, type DraftValidationResponse } from '@/lib/api'

interface JobsState {
  jobs: Job[]
  connected: boolean
  activeWorkspace: string

  // Actions
  setActiveWorkspace: (workspace: string) => void
  createJob: (request: CreateJobRequest) => Promise<string>
  cancelJob: (id: string) => Promise<void>
  clearCompleted: () => Promise<void>
  connect: () => void
  disconnect: () => void

  // Internal
  _eventSource: EventSource | null
  _updateJob: (job: Job) => void
  _removeJob: (id: string) => void
}

export const useJobsStore = create<JobsState>((set, get) => ({
  jobs: [],
  connected: false,
  activeWorkspace: 'ground',
  _eventSource: null,

  setActiveWorkspace: (workspace) => set({ activeWorkspace: workspace }),

  createJob: async (request) => {
    const { id } = await api.jobs.create(request)
    return id
  },

  cancelJob: async (id) => {
    await api.jobs.cancel(id)
  },

  clearCompleted: async () => {
    await api.jobs.clear()
    set((state) => ({
      jobs: state.jobs.filter(
        (j) => j.status !== 'Completed' && j.status !== 'Failed' && j.status !== 'Cancelled'
      ),
    }))
  },

  connect: () => {
    const state = get()
    if (state._eventSource) return // Already connected

    const es = createJobEventSource((eventType, job) => {
      const { _updateJob } = get()

      switch (eventType) {
        case 'job-sync':
        case 'job-created':
        case 'job-started':
        case 'job-progress':
        case 'job-completed':
        case 'job-failed':
          _updateJob(job)
          break
        case 'job-cancelled':
          _updateJob(job)
          break
      }
    })

    es.onopen = () => set({ connected: true })
    es.onerror = () => set({ connected: false })

    set({ _eventSource: es })
  },

  disconnect: () => {
    const state = get()
    if (state._eventSource) {
      state._eventSource.close()
      set({ _eventSource: null, connected: false })
    }
  },

  _updateJob: (job) => {
    set((state) => {
      const idx = state.jobs.findIndex((j) => j.id === job.id)
      if (idx >= 0) {
        const newJobs = [...state.jobs]
        newJobs[idx] = job
        return { jobs: newJobs }
      } else {
        return { jobs: [job, ...state.jobs] }
      }
    })
  },

  _removeJob: (id) => {
    set((state) => ({
      jobs: state.jobs.filter((j) => j.id !== id),
    }))
  },
}))

// Selector functions (use with useShallow for arrays)
const selectRunningJobs = (state: JobsState) =>
  state.jobs.filter((j) => j.status === 'Running')

const selectQueuedJobs = (state: JobsState) =>
  state.jobs.filter((j) => j.status === 'Queued')

const selectCompletedJobs = (state: JobsState) =>
  state.jobs.filter((j) => j.status === 'Completed' || j.status === 'Failed' || j.status === 'Cancelled')

// Primitive selectors (safe to use directly)
export const selectActiveJobCount = (state: JobsState) =>
  state.jobs.filter((j) => j.status === 'Running' || j.status === 'Queued').length

export const selectAggregateProgress = (state: JobsState) => {
  const running = state.jobs.filter((j) => j.status === 'Running')
  if (running.length === 0) return null

  const total = running.reduce((sum, j) => sum + (j.progressPercent ?? 0), 0)
  return Math.round(total / running.length)
}

// Hooks with shallow comparison for array selectors
export const useRunningJobs = () => useJobsStore(useShallow(selectRunningJobs))
export const useQueuedJobs = () => useJobsStore(useShallow(selectQueuedJobs))
export const useCompletedJobs = () => useJobsStore(useShallow(selectCompletedJobs))

// Helper to get typed result from a job
export function getJobResult<T>(job: Job): T | null {
  if (job.status !== 'Completed' || !job.result) return null
  return job.result as T
}

// Convenience: get validation result
export function getValidationResult(job: Job): DraftValidationResponse | null {
  if (job.type !== 'DraftValidation') return null
  return getJobResult<DraftValidationResponse>(job)
}

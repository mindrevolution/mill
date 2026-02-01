// Use relative URLs - works with any port since frontend is served from same origin
const API_BASE = import.meta.env.VITE_API_URL || ''

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  })

  if (!res.ok) {
    // Try to extract error message from JSON response
    let errorMessage = `API error: ${res.status} ${res.statusText}`
    try {
      const errorBody = await res.json()
      if (errorBody?.error) {
        errorMessage = errorBody.error
      }
    } catch {
      // Ignore JSON parse errors for error responses
    }
    throw new Error(errorMessage)
  }

  // Handle empty responses gracefully
  const text = await res.text()
  if (!text) {
    return undefined as T
  }
  return JSON.parse(text)
}

// Types matching API models
export interface Draft {
  id: string
  slug: string
  title: string
  type: 'feature' | 'bug' | 'security' | 'task'
  status: 'draft' | 'ready'
  updatedAt: string
  persona?: string
}

export interface DraftDetail {
  id: string
  slug: string
  title: string
  body: string
  bodyHtml: string
  type: 'feature' | 'bug' | 'security' | 'task'
  status: 'draft' | 'ready'
  updatedAt: string
  persona?: string
  hasRelevance?: boolean
}

export interface Issue {
  number: number
  title: string
  type: 'feature' | 'bug' | 'security' | 'task'
  status: 'open' | 'closed'
  persona?: string
  createdAt: string
}

export interface IssueDetail {
  number: number
  title: string
  titleHtml: string
  body: string
  bodyHtml: string
  type: 'feature' | 'bug' | 'security' | 'task'
  status: 'open' | 'closed'
  persona?: string
  labels: string[]
  createdAt: string
}

export interface SpecSession {
  id: string
  draftId?: string
  startedAt: string
}

export interface ChatResponse {
  content: string
  updatedDraft?: Draft
}

export type KnowledgeCategory = 'personas' | 'standards' | 'concepts' | 'design'

export interface KnowledgeItem {
  id: string
  category: KnowledgeCategory
  name: string
  description: string
  file: string
  createdAt: string
  updatedAt: string
}

export interface Observation {
  id: string
  category: KnowledgeCategory
  suggestion: string
  sources: string[]
  confidence: number
  createdAt: string
  updatedAt: string
}

export interface Run {
  id: string
  issue: number
  title: string
  status: 'running' | 'verifying' | 'done' | 'failed' | 'aborted'
  iteration: number
  maxIterations: number
  startedAt: string
}

export interface HistoryEntry {
  date: string
  issue: number
  pr?: number
  title: string
  type: 'feature' | 'bug' | 'security' | 'task'
  persona?: string
  intent: string
  outcome: 'shipped' | 'abandoned' | 'reverted'
  contextAdded?: string[]
  iterations?: number
  durationMs?: number
  prUrl?: string
}

export type BriefStage = 'spark' | 'grounded' | 'ready'

export interface Brief {
  id: string
  title: string
  stage: BriefStage
  intent: string
  createdAt: string
  updatedAt: string
  persona?: string
  concepts?: string[]
}

export interface BriefDetail extends Brief {
  content: string
}

export interface DroppedBrief {
  id: string
  essence: string
  droppedAt: string
  originalTitle: string
}

// Ground/Kickstart
export interface GroundStatus {
  isEmpty: boolean
  hasKickstart: boolean
}

export interface TemplateSummary {
  id: string
  label: string
  summary: string
  tags: string[]
}

export interface TemplateContent {
  id: string
  type: string
  content: string
}

export interface ArchetypeOverride {
  type?: string
  primaryUser?: string
  coreLoop?: string
  successMetric?: string
  monetization?: string
}

export interface StackOverride {
  frontend?: string
  backend?: string
  dataStorage?: string
  infraDeploy?: string
  testing?: string
  observability?: string
  avoid?: string
}

export interface KickstartRequest {
  name: string
  description?: string
  archetypeId: string
  stackId: string
  archetypeOverride?: ArchetypeOverride
  stackOverride?: StackOverride
}

export interface CreatedFile {
  category: string
  id: string
  path: string
}

export interface KickstartResponse {
  created: CreatedFile[]
}

// Draft Validation
export interface ValidationFinding {
  category: 'implemented' | 'moved' | 'renamed' | 'changed' | 'resolved' | 'obsolete'
  severity: 'info' | 'warning' | 'critical'
  reference: string
  expected: string
  actual: string
  impact: string
}

export interface DraftValidationResponse {
  score: number
  verdict: 'current' | 'review' | 'discard'
  findings: ValidationFinding[]
  summary: string
  recommendation: string
}

// Jobs
export type JobType = 'DraftValidation' | 'ContextWarmup' | 'Kickstart' | 'ShipRun' | 'ObservationExtraction'
export type JobStatus = 'Queued' | 'Running' | 'Completed' | 'Failed' | 'Cancelled'
export type JobQueue = 'Llm' | 'Ship'

// Ship Run
export interface ShipRunIteration {
  number: number
  signal: string
  summary?: string
  completedAt: string
}

export interface ShipRunResult {
  success: boolean
  iterations: number
  prUrl?: string
  abortReason?: string
  history: ShipRunIteration[]
}

export interface Job {
  id: string
  type: JobType
  queue: JobQueue
  status: JobStatus
  title: string
  params: Record<string, unknown>
  stage?: string
  progressPercent?: number
  result?: unknown
  error?: string
  sourceWorkspace?: string
  createdAt: string
  startedAt?: string
  completedAt?: string
}

export interface CreateJobRequest {
  type: JobType
  params: Record<string, unknown>
  sourceWorkspace?: string
}

export interface JobConfig {
  llmConcurrency: number
  shipConcurrency: number
}

// API client
export const api = {
  // Health
  health: () => request<{ status: string; version: string }>('/api/health'),

  // Project
  project: {
    get: () => request<{ configured: boolean; path?: string; name?: string }>('/api/project'),
    set: (path: string) => request<{ path: string; name: string }>('/api/project', {
      method: 'POST',
      body: JSON.stringify({ path }),
    }),
  },

  // Specs
  spec: {
    drafts: () => request<Draft[]>('/api/spec/drafts'),
    draft: (id: string) => request<DraftDetail>(`/api/spec/drafts/${id}`),
    validateDraft: (id: string) => request<DraftValidationResponse>(`/api/spec/drafts/${id}/validate`, {
      method: 'POST',
    }),
    getRelevance: (id: string) => request<DraftValidationResponse>(`/api/spec/drafts/${id}/relevance`),
    deleteRelevance: (id: string) => request<void>(`/api/spec/drafts/${id}/relevance`, {
      method: 'DELETE',
    }),
    issues: (refresh = false) => request<Issue[]>(`/api/spec/issues${refresh ? '?refresh=true' : ''}`),
    issue: (number: number, refresh = false) => request<IssueDetail>(`/api/spec/issues/${number}${refresh ? '?refresh=true' : ''}`),
    closeIssue: (number: number) => request<void>(`/api/spec/issues/${number}/close`, {
      method: 'POST',
    }),
    start: (draftId?: string, type?: string) => request<SpecSession>('/api/spec/start', {
      method: 'POST',
      body: JSON.stringify({ draftId, type }),
    }),
    message: (sessionId: string, content: string) => request<ChatResponse>('/api/spec/message', {
      method: 'POST',
      body: JSON.stringify({ sessionId, content }),
    }),
  },

  // Knowledge (legacy alias for ground)
  knowledge: {
    list: (category: string) => request<KnowledgeItem[]>(`/api/knowledge/${category}`),
    get: (category: string, id: string) => request<{ id: string; category: string; content: string }>(
      `/api/knowledge/${category}/${id}`
    ),
  },

  // Ground
  ground: {
    status: () => request<GroundStatus>('/api/ground/status'),
    archetypes: () => request<TemplateSummary[]>('/api/ground/archetypes'),
    stacks: () => request<TemplateSummary[]>('/api/ground/stacks'),
    template: (type: string, id: string) => request<TemplateContent>(`/api/ground/templates/${type}/${id}`),
    items: (category: string) => request<KnowledgeItem[]>(`/api/ground/${category}`),
    item: (category: string, id: string) => request<{ id: string; category: string; content: string }>(
      `/api/ground/${category}/${id}`
    ),
    kickstart: (data: KickstartRequest) => request<KickstartResponse>('/api/ground/kickstart', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
    // Observations
    observations: () => request<Observation[]>('/api/ground/observations'),
    acceptObservation: (id: string) => request<KnowledgeItem>(`/api/ground/observations/${id}/accept`, {
      method: 'POST',
    }),
    dismissObservation: (id: string) => request<void>(`/api/ground/observations/${id}`, {
      method: 'DELETE',
    }),
    banObservation: (id: string) => request<void>(`/api/ground/observations/${id}/ban`, {
      method: 'POST',
    }),
  },

  // Runs
  run: {
    list: () => request<Run[]>('/api/run'),
    start: (issue: number) => request<Run>(`/api/run/${issue}`, { method: 'POST' }),
    abort: (runId: string) => request<{ aborted: string }>(`/api/run/${runId}`, { method: 'DELETE' }),
    logs: (runId: string) => request<{ id: string; lines: string[] }>(`/api/run/${runId}/logs`),
  },

  // History
  history: () => request<HistoryEntry[]>('/api/history'),

  // Briefs
  brief: {
    list: () => request<Brief[]>('/api/brief'),
    get: (id: string) => request<BriefDetail>(`/api/brief/${id}`),
    create: (title: string, intent: string, type?: string) => request<Brief>('/api/brief', {
      method: 'POST',
      body: JSON.stringify({ title, intent, type }),
    }),
    update: (id: string, data: {
      title?: string
      stage?: BriefStage
      intent?: string
      persona?: string
      concepts?: string[]
      content?: string
    }) => request<Brief>(`/api/brief/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
    drop: (id: string, essence: string) => request<DroppedBrief>(`/api/brief/${id}/drop`, {
      method: 'POST',
      body: JSON.stringify({ essence }),
    }),
    promote: (id: string, type: string) => request<Draft>(`/api/brief/${id}/promote`, {
      method: 'POST',
      body: JSON.stringify({ type }),
    }),
    dropped: () => request<DroppedBrief[]>('/api/brief/dropped'),
  },

  // Jobs
  jobs: {
    list: () => request<Job[]>('/api/jobs'),
    get: (id: string) => request<Job>(`/api/jobs/${id}`),
    create: (req: CreateJobRequest) => request<{ id: string }>('/api/jobs', {
      method: 'POST',
      body: JSON.stringify(req),
    }),
    cancel: (id: string) => request<void>(`/api/jobs/${id}`, { method: 'DELETE' }),
    clear: () => request<void>('/api/jobs', { method: 'DELETE' }),
    config: () => request<JobConfig>('/api/jobs/config'),
    // SSE endpoint - use createJobEventSource() instead
  },
}

// SSE helper for job events
export function createJobEventSource(onEvent: (eventType: string, job: Job) => void): EventSource {
  const url = `${API_BASE}/api/jobs/events`
  const es = new EventSource(url)

  const handleEvent = (e: MessageEvent) => {
    try {
      const job = JSON.parse(e.data) as Job
      onEvent(e.type, job)
    } catch (err) {
      console.error('Failed to parse job event:', err)
    }
  }

  es.addEventListener('job-sync', handleEvent)
  es.addEventListener('job-created', handleEvent)
  es.addEventListener('job-started', handleEvent)
  es.addEventListener('job-progress', handleEvent)
  es.addEventListener('job-completed', handleEvent)
  es.addEventListener('job-failed', handleEvent)
  es.addEventListener('job-cancelled', handleEvent)

  return es
}

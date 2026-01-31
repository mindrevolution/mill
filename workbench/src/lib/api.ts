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
    throw new Error(`API error: ${res.status} ${res.statusText}`)
  }

  return res.json()
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

export interface KnowledgeItem {
  id: string
  category: string
  name: string
  description: string
  file: string
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
    issues: (refresh = false) => request<Issue[]>(`/api/spec/issues${refresh ? '?refresh=true' : ''}`),
    issue: (number: number, refresh = false) => request<IssueDetail>(`/api/spec/issues/${number}${refresh ? '?refresh=true' : ''}`),
    start: (draftId?: string, type?: string) => request<SpecSession>('/api/spec/start', {
      method: 'POST',
      body: JSON.stringify({ draftId, type }),
    }),
    message: (sessionId: string, content: string) => request<ChatResponse>('/api/spec/message', {
      method: 'POST',
      body: JSON.stringify({ sessionId, content }),
    }),
  },

  // Knowledge
  knowledge: {
    list: (category: string) => request<KnowledgeItem[]>(`/api/knowledge/${category}`),
    get: (category: string, id: string) => request<{ id: string; category: string; content: string }>(
      `/api/knowledge/${category}/${id}`
    ),
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
}

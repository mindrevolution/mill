// Workspace types
export type Workspace = 'shape' | 'map' | 'ship'

// Shape workspace
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
  status: 'open' | 'in-progress' | 'done'
  persona?: string
  createdAt: string
}

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: string
}

// Map workspace
export type LibraryCategory = 'personas' | 'standards' | 'concepts' | 'design'

export interface LibraryItem {
  id: string
  category: LibraryCategory
  name: string
  description: string
  file: string
  createdAt: string
  updatedAt: string
}

export interface Observation {
  id: string
  category: LibraryCategory
  suggestion: string
  source: string // what triggered this observation
  confidence: number
  createdAt: string
}

// Ship workspace
export type RunStatus = 'starting' | 'running' | 'verifying' | 'done' | 'failed' | 'aborted'

export interface Run {
  id: string
  issue: number
  title: string
  status: RunStatus
  iteration: number
  maxIterations: number
  startedAt: string
  logs: string[]
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

// Project
export interface Project {
  name: string
  path: string
  lastOpened: string
}

// Runtime events
export type RunEvent =
  | { type: 'status'; runId: string; status: RunStatus }
  | { type: 'output'; runId: string; text: string }
  | { type: 'iteration'; runId: string; current: number; max: number }
  | { type: 'tool_call'; runId: string; tool: string; args?: string }
  | { type: 'tool_result'; runId: string; tool: string; result?: string }
  | { type: 'input_needed'; runId: string; prompt: string }
  | { type: 'error'; runId: string; message: string }

export interface RunHandle {
  id: string
  issue: number
  status: RunStatus
}

export type RuntimeType = 'mock' | 'pty' | 'api'

// Non-interactive task execution (ApiRuntime)
export type TaskType =
  | 'add-observation'
  | 'context-warmup'
  | 'verify-criterion'
  | 'refine-spec'

export interface TaskRequest {
  type: TaskType
  prompt: string
  context?: Record<string, unknown>
  outputFormat?: 'text' | 'json'
}

export interface TaskResult {
  success: boolean
  output: string
  data?: unknown  // Parsed JSON if outputFormat was 'json'
  error?: string
}

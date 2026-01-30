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
export interface Run {
  id: string
  issue: number
  title: string
  status: 'running' | 'verifying' | 'done' | 'failed' | 'aborted'
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

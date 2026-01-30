import type { RunEvent, RunHandle, RunStatus, RuntimeType } from '@/types'

export interface Runtime {
  /** Start a run for the given issue */
  start(issue: number): Promise<RunHandle>

  /** Send input to a running session (when input_needed) */
  send(runId: string, input: string): Promise<void>

  /** Abort a running session */
  abort(runId: string): Promise<void>

  /** Subscribe to events for a run (returns unsubscribe fn) */
  subscribe(runId: string, handler: (event: RunEvent) => void): () => void

  /** Get current status of a run */
  status(runId: string): RunStatus | null
}

let currentRuntime: Runtime | null = null

export function setRuntime(runtime: Runtime): void {
  currentRuntime = runtime
}

export function getRuntime(): Runtime {
  if (!currentRuntime) {
    throw new Error('Runtime not initialized. Call setRuntime() first.')
  }
  return currentRuntime
}

export function detectRuntime(): RuntimeType {
  // Check for Tauri environment
  if (typeof window !== 'undefined' && '__TAURI__' in window) {
    return 'tauri'
  }

  // In production (not localhost), use API
  if (typeof window !== 'undefined' && !window.location.hostname.includes('localhost')) {
    return 'api'
  }

  // Default to mock for development
  return 'mock'
}

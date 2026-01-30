import type { RunEvent, RunHandle, RunStatus, RuntimeType, TaskRequest, TaskResult } from '@/types'

/**
 * Runtime interface for Claude Code CLI integration.
 *
 * Two implementations with different purposes:
 * - PtyRuntime: Interactive sessions (embedded PTY) - spec elicitation, work loop
 * - ApiRuntime: Non-interactive tasks (HTTP → CLI spawn) - observations, warmup, verification
 */
export interface Runtime {
  // === Interactive methods (PtyRuntime) ===

  /** Start an interactive run for the given issue */
  start(issue: number): Promise<RunHandle>

  /** Send input to a running session (when input_needed) */
  send(runId: string, input: string): Promise<void>

  /** Abort a running session */
  abort(runId: string): Promise<void>

  /** Subscribe to events for a run (returns unsubscribe fn) */
  subscribe(runId: string, handler: (event: RunEvent) => void): () => void

  /** Get current status of a run */
  status(runId: string): RunStatus | null

  // === Non-interactive methods (ApiRuntime) ===

  /**
   * Execute a non-interactive task via API.
   * API spawns: claude --print "prompt" --output-format json
   * Returns structured result.
   */
  execute?(task: TaskRequest): Promise<TaskResult>
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
  // Check for Photino environment (window.external.sendMessage available)
  if (typeof window !== 'undefined' && 'external' in window && typeof (window as any).external?.sendMessage === 'function') {
    return 'pty'
  }

  // In production (not localhost), use API
  if (typeof window !== 'undefined' && !window.location.hostname.includes('localhost')) {
    return 'api'
  }

  // Default to mock for development
  return 'mock'
}

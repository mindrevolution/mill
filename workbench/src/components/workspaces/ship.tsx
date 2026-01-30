import { useState, useEffect, useRef } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { useRuntime } from '@/hooks/useRuntime'
import type { HistoryEntry, RunStatus, RunEvent } from '@/types'
import {
  Loader2,
  CheckCircle2,
  XCircle,
  StopCircle,
  Clock,
  FileText,
  Bug,
  Shield,
  Wrench,
  ExternalLink,
  GitPullRequest,
  History,
  Rocket,
  Plus,
  Terminal,
} from 'lucide-react'

// Live run state derived from runtime events
interface LiveRun {
  id: string
  issue: number
  status: RunStatus
  iteration: number
  maxIterations: number
  startedAt: Date
  logs: string[]
}

const typeIcons = {
  feature: FileText,
  bug: Bug,
  security: Shield,
  task: Wrench,
}

const statusConfig: Record<RunStatus, { icon: typeof Loader2; color: string; label: string; animate: boolean }> = {
  starting: { icon: Loader2, color: 'text-muted-foreground', label: 'Starting', animate: true },
  running: { icon: Loader2, color: 'text-blue-400', label: 'Running', animate: true },
  verifying: { icon: Loader2, color: 'text-yellow-400', label: 'Verifying', animate: true },
  done: { icon: CheckCircle2, color: 'text-green-400', label: 'Done', animate: false },
  failed: { icon: XCircle, color: 'text-red-400', label: 'Failed', animate: false },
  aborted: { icon: StopCircle, color: 'text-muted-foreground', label: 'Aborted', animate: false },
}

const outcomeConfig = {
  shipped: { color: 'text-green-400', bg: 'bg-green-400/10' },
  abandoned: { color: 'text-muted-foreground', bg: 'bg-muted' },
  reverted: { color: 'text-red-400', bg: 'bg-red-400/10' },
}

// Helper to format event as log line
function eventToLog(event: RunEvent): string | null {
  switch (event.type) {
    case 'output':
      return event.text
    case 'tool_call':
      return `• ${event.tool}${event.args ? ` ${event.args}` : ''}`
    case 'tool_result':
      return `  ↳ ${event.result || 'ok'}`
    case 'status':
      return `• status: ${event.status}`
    case 'iteration':
      return null // handled separately
    case 'error':
      return `✕ ${event.message}`
    case 'input_needed':
      return `❯ ${event.prompt}`
    default:
      return null
  }
}

// Helper to format relative time
function formatRelativeTime(date: Date): string {
  const seconds = Math.floor((Date.now() - date.getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  return `${hours}h ago`
}

const mockHistory: HistoryEntry[] = [
  {
    date: '2026-01-30',
    issue: 40,
    pr: 45,
    title: 'Refactor auth module',
    type: 'task',
    intent: 'Clean up authentication code for better maintainability',
    outcome: 'shipped',
    contextAdded: ['standard:error-handling'],
  },
  {
    date: '2026-01-28',
    issue: 38,
    pr: 42,
    title: 'Fix session expiry bug',
    type: 'bug',
    persona: 'mobile-user',
    intent: 'Users reported being logged out unexpectedly on mobile',
    outcome: 'shipped',
  },
  {
    date: '2026-01-25',
    issue: 35,
    pr: 39,
    title: 'Add user preferences page',
    type: 'feature',
    persona: 'power-user',
    intent: 'Allow users to customize their experience',
    outcome: 'shipped',
    contextAdded: ['concept:preferences', 'persona:power-user'],
  },
  {
    date: '2026-01-20',
    issue: 32,
    title: 'Implement WebSocket reconnection',
    type: 'feature',
    intent: 'Handle network interruptions gracefully',
    outcome: 'abandoned',
  },
]

function ActiveRuns({
  runs,
  onSelect,
  selectedId,
  onStartDemo,
}: {
  runs: LiveRun[]
  onSelect: (run: LiveRun) => void
  selectedId?: string
  onStartDemo: () => void
}) {
  if (runs.length === 0) {
    return (
      <div className="h-full flex flex-col">
        <div className="p-3 border-b flex items-center justify-between">
          <span className="text-sm font-medium">Active Runs</span>
          <Button size="sm" variant="outline" className="h-7" onClick={onStartDemo}>
            <Plus className="h-3 w-3 mr-1" />
            Demo
          </Button>
        </div>
        <div className="flex-1 flex items-center justify-center text-muted-foreground">
          <div className="text-center">
            <Rocket className="h-8 w-8 mx-auto mb-2 opacity-50" />
            <p className="text-sm">No active runs</p>
            <p className="text-xs mt-1">Click Demo to simulate a run</p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <span className="text-sm font-medium">Active Runs</span>
        <Button size="sm" variant="outline" className="h-7" onClick={onStartDemo}>
          <Plus className="h-3 w-3 mr-1" />
          Demo
        </Button>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-2">
          {runs.map((run) => {
            const status = statusConfig[run.status]
            const StatusIcon = status.icon

            return (
              <Button
                key={run.id}
                onClick={() => onSelect(run)}
                variant={selectedId === run.id ? 'secondary' : 'ghost'}
                size="sm"
                className={cn(
                  'h-auto w-full justify-start p-3 text-left border',
                  selectedId === run.id ? 'border-primary/50' : 'border-transparent hover:border-muted-foreground/30'
                )}
              >
                <div className="w-full">
                  <div className="flex items-center gap-2 mb-2">
                    <StatusIcon
                      className={cn(
                        'h-4 w-4',
                        status.color,
                        status.animate && 'animate-spin'
                      )}
                    />
                    <span className="text-xs text-muted-foreground">#{run.issue}</span>
                    <span className="text-sm font-medium truncate flex-1">Issue #{run.issue}</span>
                  </div>
                  <div className="flex items-center justify-between text-xs text-muted-foreground">
                    <span>Iteration {run.iteration}/{run.maxIterations}</span>
                    <span>{formatRelativeTime(run.startedAt)}</span>
                  </div>
                  {/* Progress bar */}
                  <div className="mt-2 h-1 bg-secondary rounded-full overflow-hidden">
                    <div
                      className={cn('h-full transition-all', status.color.replace('text-', 'bg-'))}
                      style={{ width: `${(run.iteration / run.maxIterations) * 100}%` }}
                    />
                  </div>
                </div>
              </Button>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

function RunDetail({ run, onAbort }: { run?: LiveRun; onAbort: () => void }) {
  const scrollRef = useRef<HTMLDivElement>(null)
  const isTerminal = run?.status === 'done' || run?.status === 'failed' || run?.status === 'aborted'

  // Auto-scroll to bottom when new logs arrive
  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [run?.logs.length])

  if (!run) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Terminal className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">Select a run to view logs</p>
        </div>
      </div>
    )
  }

  const status = statusConfig[run.status]

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="p-3 border-b">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <Badge variant="secondary">#{run.issue}</Badge>
            <span className="font-medium">Issue #{run.issue}</span>
          </div>
          {!isTerminal && (
            <Button size="sm" variant="destructive" className="h-7" onClick={onAbort}>
              <StopCircle className="h-3 w-3 mr-1" />
              Abort
            </Button>
          )}
        </div>
        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          <span className={status.color}>{status.label}</span>
          <span>Iteration {run.iteration}/{run.maxIterations}</span>
          <span>Started {formatRelativeTime(run.startedAt)}</span>
        </div>
      </div>

      {/* Logs */}
      <ScrollArea className="flex-1" ref={scrollRef}>
        <div className="p-3 font-mono text-xs space-y-1">
          {run.logs.map((log, i) => (
            <div key={i} className="text-muted-foreground whitespace-pre-wrap">
              {log}
            </div>
          ))}
          {(run.status === 'running' || run.status === 'starting') && (
            <div className="flex items-center gap-2 text-muted-foreground">
              <Loader2 className="h-3 w-3 animate-spin" />
              <span>working...</span>
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  )
}

function HistoryList({
  entries,
  onSelect,
  selectedDate,
}: {
  entries: HistoryEntry[]
  onSelect: (entry: HistoryEntry) => void
  selectedDate?: string
}) {
  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <div className="flex items-center gap-2">
          <History className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm font-medium">History</span>
        </div>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-1">
          {entries.map((entry) => {
            const TypeIcon = typeIcons[entry.type]
            const outcome = outcomeConfig[entry.outcome]

            return (
              <Button
                key={`${entry.date}-${entry.issue}`}
                onClick={() => onSelect(entry)}
                variant={selectedDate === entry.date ? 'secondary' : 'ghost'}
                size="sm"
                className="h-auto w-full justify-start p-3 text-left"
              >
                <div className="w-full">
                  <div className="flex items-center gap-2 mb-1">
                    <TypeIcon className="h-3 w-3 text-muted-foreground" />
                    <span className="text-xs text-muted-foreground">#{entry.issue}</span>
                    <span className="text-sm truncate flex-1">{entry.title}</span>
                    <Badge variant="secondary" className={cn('text-[10px]', outcome.color, outcome.bg)}>
                      {entry.outcome}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span>{entry.date}</span>
                    {entry.persona && <span>· {entry.persona}</span>}
                    {entry.pr && (
                      <span className="flex items-center gap-1">
                        · <GitPullRequest className="h-3 w-3" /> #{entry.pr}
                      </span>
                    )}
                  </div>
                </div>
              </Button>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

function HistoryDetail({ entry }: { entry?: HistoryEntry }) {
  if (!entry) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Clock className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">Select an entry to view details</p>
        </div>
      </div>
    )
  }

  const TypeIcon = typeIcons[entry.type]
  const outcome = outcomeConfig[entry.outcome]

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-4">
        <div>
          <div className="flex items-center gap-2 mb-2">
            <TypeIcon className="h-4 w-4 text-muted-foreground" />
            <Badge variant="secondary">{entry.type}</Badge>
            <Badge className={cn(outcome.color, outcome.bg)}>{entry.outcome}</Badge>
          </div>
          <h2 className="text-lg font-semibold">{entry.title}</h2>
          <p className="text-sm text-muted-foreground mt-1">#{entry.issue}</p>
        </div>

        <Separator />

        <div>
          <h3 className="text-sm font-medium mb-2">Intent</h3>
          <p className="text-sm text-muted-foreground">{entry.intent}</p>
        </div>

        {entry.persona && (
          <div>
            <h3 className="text-sm font-medium mb-2">Persona</h3>
            <Badge variant="secondary">{entry.persona}</Badge>
          </div>
        )}

        {entry.contextAdded && entry.contextAdded.length > 0 && (
          <div>
            <h3 className="text-sm font-medium mb-2">Context Added</h3>
            <div className="flex flex-wrap gap-1">
              {entry.contextAdded.map((ctx) => (
                <Badge key={ctx} variant="outline" className="text-xs">
                  {ctx}
                </Badge>
              ))}
            </div>
          </div>
        )}

        <Separator />

        <div className="flex gap-2">
          {entry.pr && (
            <Button variant="outline" className="flex-1">
              <GitPullRequest className="h-4 w-4 mr-2" />
              View PR #{entry.pr}
              <ExternalLink className="h-3 w-3 ml-2" />
            </Button>
          )}
          <Button variant="outline">
            View Issue
            <ExternalLink className="h-3 w-3 ml-2" />
          </Button>
        </div>
      </div>
    </ScrollArea>
  )
}

export function ShipWorkspace() {
  const runtime = useRuntime()
  const [runs, setRuns] = useState<Map<string, LiveRun>>(new Map())
  const [selectedRunId, setSelectedRunId] = useState<string | undefined>()
  const [selectedHistory, setSelectedHistory] = useState<HistoryEntry | undefined>()
  const [focusedTile, setFocusedTile] = useState<'runs' | 'logs' | 'history' | 'detail'>('runs')
  const [demoIssue, setDemoIssue] = useState(42)

  const selectedRun = selectedRunId ? runs.get(selectedRunId) : undefined
  const activeRuns = Array.from(runs.values()).filter(
    (r) => r.status !== 'done' && r.status !== 'failed' && r.status !== 'aborted'
  )

  // Subscribe to run events when a run is started
  const subscribeToRun = (runId: string, issue: number) => {
    const liveRun: LiveRun = {
      id: runId,
      issue,
      status: 'starting',
      iteration: 1,
      maxIterations: 5,
      startedAt: new Date(),
      logs: [],
    }
    setRuns((prev) => new Map(prev).set(runId, liveRun))

    return runtime.subscribe(runId, (event) => {
      setRuns((prev) => {
        const updated = new Map(prev)
        const run = updated.get(runId)
        if (!run) return prev

        const newRun = { ...run }

        if (event.type === 'status') {
          newRun.status = event.status
        } else if (event.type === 'iteration') {
          newRun.iteration = event.current
          newRun.maxIterations = event.max
        }

        const logLine = eventToLog(event)
        if (logLine) {
          newRun.logs = [...newRun.logs, logLine]
        }

        updated.set(runId, newRun)
        return updated
      })
    })
  }

  const handleStartDemo = async () => {
    const handle = await runtime.start(demoIssue)
    subscribeToRun(handle.id, handle.issue)
    setSelectedRunId(handle.id)
    setFocusedTile('logs')
    setDemoIssue((prev) => prev + 1) // increment for next demo
  }

  const handleAbort = () => {
    if (selectedRunId) {
      runtime.abort(selectedRunId)
    }
  }

  return (
    <div className="h-full p-1">
      <TileSplit direction="vertical" sizes={[50, 50]}>
        {/* Top: Active runs */}
        <TileSplit direction="horizontal" sizes={[40, 60]}>
          <Tile
            focused={focusedTile === 'runs'}
            onFocus={() => setFocusedTile('runs')}
          >
            <ActiveRuns
              runs={activeRuns}
              selectedId={selectedRunId}
              onSelect={(run) => {
                setSelectedRunId(run.id)
                setFocusedTile('logs')
              }}
              onStartDemo={handleStartDemo}
            />
          </Tile>
          <Tile
            focused={focusedTile === 'logs'}
            onFocus={() => setFocusedTile('logs')}
          >
            <RunDetail run={selectedRun} onAbort={handleAbort} />
          </Tile>
        </TileSplit>

        {/* Bottom: History */}
        <TileSplit direction="horizontal" sizes={[50, 50]}>
          <Tile
            focused={focusedTile === 'history'}
            onFocus={() => setFocusedTile('history')}
          >
            <HistoryList
              entries={mockHistory}
              selectedDate={selectedHistory?.date}
              onSelect={(entry) => {
                setSelectedHistory(entry)
                setFocusedTile('detail')
              }}
            />
          </Tile>
          <Tile
            focused={focusedTile === 'detail'}
            onFocus={() => setFocusedTile('detail')}
          >
            <HistoryDetail entry={selectedHistory} />
          </Tile>
        </TileSplit>
      </TileSplit>
    </div>
  )
}

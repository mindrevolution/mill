import { useState } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import type { Run, HistoryEntry, RunStatus } from '@/types'
import {
  Play,
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
} from 'lucide-react'

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

// Mock data
const mockRuns: Run[] = [
  {
    id: '1',
    issue: 42,
    title: 'Implement dark mode toggle',
    status: 'running',
    iteration: 2,
    maxIterations: 5,
    startedAt: '10 minutes ago',
    logs: [
      '• reading spec from issue #42...',
      '✓ spec loaded',
      '• implementing dark mode toggle...',
      '  ↳ added ThemeContext provider',
      '  ↳ created useTheme hook',
      '• running tests...',
    ],
  },
  {
    id: '2',
    issue: 41,
    title: 'API rate limiting',
    status: 'verifying',
    iteration: 3,
    maxIterations: 5,
    startedAt: '25 minutes ago',
    logs: [
      '• reading spec from issue #41...',
      '✓ spec loaded',
      '• implementing rate limiter...',
      '✓ implementation complete',
      '• verifying against acceptance criteria...',
    ],
  },
]

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

function ActiveRuns({ runs, onSelect, selectedId }: { runs: Run[]; onSelect: (run: Run) => void; selectedId?: string }) {
  if (runs.length === 0) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Rocket className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">No active runs</p>
          <p className="text-xs mt-1">Start a run with `mill run #issue`</p>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b">
        <span className="text-sm font-medium">Active Runs</span>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-2">
          {runs.map((run) => {
            const status = statusConfig[run.status]
            const StatusIcon = status.icon

            return (
              <button
                key={run.id}
                onClick={() => onSelect(run)}
                className={cn(
                  'w-full text-left p-3 rounded-lg border transition-colors',
                  selectedId === run.id ? 'bg-card border-primary/50' : 'hover:bg-card/50 border-transparent'
                )}
              >
                <div className="flex items-center gap-2 mb-2">
                  <StatusIcon
                    className={cn(
                      'h-4 w-4',
                      status.color,
                      status.animate && 'animate-spin'
                    )}
                  />
                  <span className="text-xs text-muted-foreground">#{run.issue}</span>
                  <span className="text-sm font-medium truncate flex-1">{run.title}</span>
                </div>
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  <span>Iteration {run.iteration}/{run.maxIterations}</span>
                  <span>{run.startedAt}</span>
                </div>
                {/* Progress bar */}
                <div className="mt-2 h-1 bg-secondary rounded-full overflow-hidden">
                  <div
                    className={cn('h-full transition-all', status.color.replace('text-', 'bg-'))}
                    style={{ width: `${(run.iteration / run.maxIterations) * 100}%` }}
                  />
                </div>
              </button>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

function RunDetail({ run }: { run?: Run }) {
  if (!run) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Play className="h-8 w-8 mx-auto mb-2 opacity-50" />
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
            <span className="font-medium">{run.title}</span>
          </div>
          <Button size="sm" variant="destructive" className="h-7">
            <StopCircle className="h-3 w-3 mr-1" />
            Abort
          </Button>
        </div>
        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          <span className={status.color}>{status.label}</span>
          <span>Iteration {run.iteration}/{run.maxIterations}</span>
          <span>Started {run.startedAt}</span>
        </div>
      </div>

      {/* Logs */}
      <ScrollArea className="flex-1">
        <div className="p-3 font-mono text-xs space-y-1">
          {run.logs.map((log, i) => (
            <div key={i} className="text-muted-foreground">
              {log}
            </div>
          ))}
          {run.status === 'running' && (
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
              <button
                key={`${entry.date}-${entry.issue}`}
                onClick={() => onSelect(entry)}
                className={cn(
                  'w-full text-left p-3 rounded-lg transition-colors',
                  selectedDate === entry.date ? 'bg-card' : 'hover:bg-card/50'
                )}
              >
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
              </button>
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
  const [selectedRun, setSelectedRun] = useState<Run | undefined>(mockRuns[0])
  const [selectedHistory, setSelectedHistory] = useState<HistoryEntry | undefined>()
  const [focusedTile, setFocusedTile] = useState<'runs' | 'logs' | 'history' | 'detail'>('runs')

  return (
    <div className="h-full p-1">
      <TileSplit direction="vertical" sizes={[50, 50]}>
        {/* Top: Active runs */}
        <TileSplit direction="horizontal" sizes={[40, 60]}>
          <Tile
            title="Runs"
            focused={focusedTile === 'runs'}
            onFocus={() => setFocusedTile('runs')}
          >
            <ActiveRuns
              runs={mockRuns}
              selectedId={selectedRun?.id}
              onSelect={(run) => {
                setSelectedRun(run)
                setFocusedTile('logs')
              }}
            />
          </Tile>
          <Tile
            title="Logs"
            focused={focusedTile === 'logs'}
            onFocus={() => setFocusedTile('logs')}
          >
            <RunDetail run={selectedRun} />
          </Tile>
        </TileSplit>

        {/* Bottom: History */}
        <TileSplit direction="horizontal" sizes={[50, 50]}>
          <Tile
            title="History"
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
            title="Detail"
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

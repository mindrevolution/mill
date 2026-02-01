import { useState, useEffect } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { useJobsStore } from '@/stores/jobs'
import { api, type Job, type HistoryEntry } from '@/lib/api'
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
  RefreshCw,
} from 'lucide-react'

const typeIcons: Record<string, typeof FileText> = {
  feature: FileText,
  bug: Bug,
  security: Shield,
  task: Wrench,
}

const jobStatusConfig: Record<string, { icon: typeof Loader2; color: string; label: string; animate: boolean }> = {
  Queued: { icon: Clock, color: 'text-muted-foreground', label: 'Queued', animate: false },
  Running: { icon: Loader2, color: 'text-blue-400', label: 'Running', animate: true },
  Completed: { icon: CheckCircle2, color: 'text-green-400', label: 'Completed', animate: false },
  Failed: { icon: XCircle, color: 'text-red-400', label: 'Failed', animate: false },
  Cancelled: { icon: StopCircle, color: 'text-muted-foreground', label: 'Cancelled', animate: false },
}

const outcomeConfig: Record<string, { color: string; bg: string }> = {
  shipped: { color: 'text-green-400', bg: 'bg-green-400/10' },
  abandoned: { color: 'text-muted-foreground', bg: 'bg-muted' },
  reverted: { color: 'text-red-400', bg: 'bg-red-400/10' },
}

// Helper to format elapsed time (mm:ss or h:mm:ss)
function formatElapsed(startedAt: string): string {
  const start = new Date(startedAt).getTime()
  const now = Date.now()
  const totalSeconds = Math.floor((now - start) / 1000)

  if (totalSeconds < 0) return '0:00'

  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
  }
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}

// Component that shows live elapsed time for running jobs
function ElapsedTime({ startedAt }: { startedAt: string }) {
  const [elapsed, setElapsed] = useState(formatElapsed(startedAt))

  useEffect(() => {
    const interval = setInterval(() => {
      setElapsed(formatElapsed(startedAt))
    }, 1000)
    return () => clearInterval(interval)
  }, [startedAt])

  return <span className="font-mono tabular-nums">{elapsed}</span>
}

// Helper to format relative time
function formatRelativeTime(dateStr: string): string {
  const date = new Date(dateStr)
  const seconds = Math.floor((Date.now() - date.getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  return `${days}d ago`
}

// Helper to format duration
function formatDuration(ms: number): string {
  const seconds = Math.floor(ms / 1000)
  if (seconds < 60) return `${seconds}s`
  const minutes = Math.floor(seconds / 60)
  const remainingSeconds = seconds % 60
  if (minutes < 60) return `${minutes}m ${remainingSeconds}s`
  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60
  return `${hours}h ${remainingMinutes}m`
}

function ActiveRuns({
  jobs,
  onSelect,
  selectedId,
}: {
  jobs: Job[]
  onSelect: (job: Job) => void
  selectedId?: string
}) {
  if (jobs.length === 0) {
    return (
      <div className="h-full flex flex-col">
        <div className="p-3 border-b flex items-center justify-between">
          <span className="text-sm font-medium">Active Runs</span>
        </div>
        <div className="flex-1 flex items-center justify-center text-muted-foreground">
          <div className="text-center">
            <Rocket className="h-8 w-8 mx-auto mb-2 opacity-50" />
            <p className="text-sm">No active runs</p>
            <p className="text-xs mt-1">Start a run from the Shape workspace</p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <span className="text-sm font-medium">Active Runs</span>
        <Badge variant="secondary">{jobs.length}</Badge>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-2">
          {jobs.map((job) => {
            const status = jobStatusConfig[job.status] || jobStatusConfig.Running
            const StatusIcon = status.icon
            const issueNumber = job.params.issueNumber as number
            const issueTitle = job.params.issueTitle as string

            return (
              <Button
                key={job.id}
                onClick={() => onSelect(job)}
                variant={selectedId === job.id ? 'secondary' : 'ghost'}
                size="sm"
                className={cn(
                  'h-auto w-full justify-start p-3 text-left border',
                  selectedId === job.id ? 'border-primary/50' : 'border-transparent hover:border-muted-foreground/30'
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
                    <span className="text-xs text-muted-foreground">#{issueNumber}</span>
                    <span className="text-sm font-medium truncate flex-1">{issueTitle || `Issue #${issueNumber}`}</span>
                  </div>
                  <div className="flex items-center justify-between text-xs text-muted-foreground gap-2">
                    <span className="flex items-center gap-1.5 truncate">
                      {job.status === 'Running' && (
                        <span className="relative flex h-2 w-2">
                          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
                          <span className="relative inline-flex rounded-full h-2 w-2 bg-blue-500"></span>
                        </span>
                      )}
                      <span className="truncate">{job.stage || status.label}</span>
                    </span>
                    {job.status === 'Running' && job.startedAt ? (
                      <ElapsedTime startedAt={job.startedAt} />
                    ) : (
                      <span className="shrink-0">{job.startedAt ? formatRelativeTime(job.startedAt) : 'queued'}</span>
                    )}
                  </div>
                  {/* Progress bar */}
                  {job.progressPercent != null && (
                    <div className="mt-2 h-1 bg-secondary rounded-full overflow-hidden">
                      <div
                        className={cn('h-full transition-all', status.color.replace('text-', 'bg-'))}
                        style={{ width: `${job.progressPercent}%` }}
                      />
                    </div>
                  )}
                </div>
              </Button>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

function RunDetail({ job, onCancel }: { job?: Job; onCancel: (jobId: string) => void }) {
  if (!job) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Rocket className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">Select a run to view details</p>
        </div>
      </div>
    )
  }

  const status = jobStatusConfig[job.status] || jobStatusConfig.Running
  const isTerminal = job.status === 'Completed' || job.status === 'Failed' || job.status === 'Cancelled'
  const issueNumber = job.params.issueNumber as number
  const issueTitle = job.params.issueTitle as string

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="p-3 border-b">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <Badge variant="secondary">#{issueNumber}</Badge>
            <span className="font-medium truncate">{issueTitle || `Issue #${issueNumber}`}</span>
          </div>
          {!isTerminal && (
            <Button size="sm" variant="destructive" className="h-7" onClick={() => onCancel(job.id)}>
              <StopCircle className="h-3 w-3 mr-1" />
              Cancel
            </Button>
          )}
        </div>
        <div className="flex items-center gap-4 text-xs text-muted-foreground flex-wrap">
          <span className={status.color}>{status.label}</span>
          {job.stage && job.status === 'Running' && (
            <span className="flex items-center gap-1.5">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-blue-500"></span>
              </span>
              {job.stage}
            </span>
          )}
          {job.stage && job.status !== 'Running' && <span>{job.stage}</span>}
          {job.startedAt && (
            job.status === 'Running' ? (
              <span>Running for <ElapsedTime startedAt={job.startedAt} /></span>
            ) : (
              <span>Started {formatRelativeTime(job.startedAt)}</span>
            )
          )}
        </div>
      </div>

      {/* Content */}
      <ScrollArea className="flex-1">
        <div className="p-4 space-y-4">
          {/* Progress */}
          {job.progressPercent != null && (
            <div>
              <div className="flex items-center justify-between text-sm mb-2">
                <span>Progress</span>
                <span>{job.progressPercent}%</span>
              </div>
              <div className="h-2 bg-secondary rounded-full overflow-hidden">
                <div
                  className={cn('h-full transition-all', status.color.replace('text-', 'bg-'))}
                  style={{ width: `${job.progressPercent}%` }}
                />
              </div>
            </div>
          )}

          {/* Live iterations for running jobs */}
          {job.status === 'Running' && job.iterations && job.iterations.length > 0 && (
            <div>
              <h4 className="text-sm font-medium mb-2">Completed Slices</h4>
              <div className="space-y-1.5">
                {job.iterations.map((iter) => (
                  <div key={iter.number} className="flex items-start gap-2 text-sm">
                    <CheckCircle2 className="h-4 w-4 text-green-400 mt-0.5 shrink-0" />
                    <div className="flex-1 min-w-0">
                      <span className="text-muted-foreground">Slice {iter.number}:</span>{' '}
                      <span className="text-foreground">{iter.summary || iter.signal}</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Error */}
          {job.error ? (
            <div className="p-3 bg-destructive/10 border border-destructive/20 rounded-md">
              <h4 className="text-sm font-medium text-destructive mb-1">Error</h4>
              <p className="text-sm text-destructive/80">{job.error}</p>
            </div>
          ) : null}

          {/* Result for completed jobs */}
          {job.status === 'Completed' && job.result != null && (
            <div className="space-y-2">
              <h4 className="text-sm font-medium">Result</h4>
              {(() => {
                const result = job.result as { success?: boolean; prUrl?: string; iterations?: number; abortReason?: string; history?: Array<{ number: number; signal: string; summary?: string }> }
                return (
                  <div className="space-y-2">
                    {result.success ? (
                      <div className="flex items-center gap-2 text-green-400">
                        <CheckCircle2 className="h-4 w-4" />
                        <span>Successfully completed</span>
                      </div>
                    ) : (
                      <div className="flex items-center gap-2 text-red-400">
                        <XCircle className="h-4 w-4" />
                        <span>{result.abortReason || 'Failed'}</span>
                      </div>
                    )}
                    {result.iterations && (
                      <p className="text-sm text-muted-foreground">Completed in {result.iterations} iteration(s)</p>
                    )}
                    {result.prUrl && (
                      <Button variant="outline" size="sm" asChild>
                        <a href={result.prUrl} target="_blank" rel="noopener noreferrer">
                          <GitPullRequest className="h-4 w-4 mr-2" />
                          View PR
                          <ExternalLink className="h-3 w-3 ml-2" />
                        </a>
                      </Button>
                    )}

                    {/* Iteration history */}
                    {result.history && result.history.length > 0 && (
                      <div className="mt-3 pt-3 border-t">
                        <h5 className="text-xs font-medium text-muted-foreground mb-2">Iteration History</h5>
                        <div className="space-y-1">
                          {result.history.map((iter) => (
                            <div key={iter.number} className="flex items-start gap-2 text-xs">
                              <span className="text-muted-foreground w-4">{iter.number}.</span>
                              <span className={cn(
                                iter.signal === 'MILL_DONE' && 'text-green-400',
                                iter.signal === 'MILL_REJECTED' && 'text-amber-400',
                                (iter.signal === 'ERROR' || iter.signal === 'UNKNOWN') && 'text-red-400'
                              )}>
                                {iter.summary || iter.signal}
                              </span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                )
              })()}
            </div>
          )}

          {/* Timestamps */}
          <div className="text-xs text-muted-foreground space-y-1">
            <p>Created: {new Date(job.createdAt).toLocaleString()}</p>
            {job.startedAt && <p>Started: {new Date(job.startedAt).toLocaleString()}</p>}
            {job.completedAt && <p>Completed: {new Date(job.completedAt).toLocaleString()}</p>}
          </div>
        </div>
      </ScrollArea>
    </div>
  )
}

function HistoryList({
  entries,
  loading,
  onSelect,
  onRefresh,
  selectedIssue,
}: {
  entries: HistoryEntry[]
  loading: boolean
  onSelect: (entry: HistoryEntry) => void
  onRefresh: () => void
  selectedIssue?: number
}) {
  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <div className="flex items-center gap-2">
          <History className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm font-medium">History</span>
        </div>
        <Button size="sm" variant="ghost" className="h-7 w-7 p-0" onClick={onRefresh}>
          <RefreshCw className={cn('h-3 w-3', loading && 'animate-spin')} />
        </Button>
      </div>
      {entries.length === 0 ? (
        <div className="flex-1 flex items-center justify-center text-muted-foreground">
          <div className="text-center">
            <Clock className="h-8 w-8 mx-auto mb-2 opacity-50" />
            <p className="text-sm">No history yet</p>
            <p className="text-xs mt-1">Completed runs will appear here</p>
          </div>
        </div>
      ) : (
        <ScrollArea className="flex-1">
          <div className="p-2 space-y-1">
            {entries.map((entry, idx) => {
              const TypeIcon = typeIcons[entry.type] || FileText
              const outcome = outcomeConfig[entry.outcome] || outcomeConfig.abandoned

              return (
                <Button
                  key={`${entry.date}-${entry.issue}-${idx}`}
                  onClick={() => onSelect(entry)}
                  variant={selectedIssue === entry.issue ? 'secondary' : 'ghost'}
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
                      <span>{new Date(entry.date).toLocaleDateString()}</span>
                      {entry.persona && <span>· {entry.persona}</span>}
                      {entry.pr && (
                        <span className="flex items-center gap-1">
                          · <GitPullRequest className="h-3 w-3" /> #{entry.pr}
                        </span>
                      )}
                      {entry.iterations && <span>· {entry.iterations} iter</span>}
                    </div>
                  </div>
                </Button>
              )
            })}
          </div>
        </ScrollArea>
      )}
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

  const TypeIcon = typeIcons[entry.type] || FileText
  const outcome = outcomeConfig[entry.outcome] || outcomeConfig.abandoned

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

        {entry.iterations && (
          <div>
            <h3 className="text-sm font-medium mb-2">Execution</h3>
            <div className="text-sm text-muted-foreground space-y-1">
              <p>Iterations: {entry.iterations}</p>
              {entry.durationMs && <p>Duration: {formatDuration(entry.durationMs)}</p>}
            </div>
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
          {entry.prUrl ? (
            <Button variant="outline" className="flex-1" asChild>
              <a href={entry.prUrl} target="_blank" rel="noopener noreferrer">
                <GitPullRequest className="h-4 w-4 mr-2" />
                View PR #{entry.pr}
                <ExternalLink className="h-3 w-3 ml-2" />
              </a>
            </Button>
          ) : entry.pr ? (
            <Button variant="outline" className="flex-1">
              <GitPullRequest className="h-4 w-4 mr-2" />
              PR #{entry.pr}
            </Button>
          ) : null}
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
  const jobs = useJobsStore((s) => s.jobs)
  const cancelJob = useJobsStore((s) => s.cancelJob)
  const viewingJobId = useJobsStore((s) => s.viewingJobId)
  const setViewingJob = useJobsStore((s) => s.setViewingJob)

  const [selectedJobId, setSelectedJobId] = useState<string | undefined>()
  const [history, setHistory] = useState<HistoryEntry[]>([])
  const [historyLoading, setHistoryLoading] = useState(false)
  const [selectedHistory, setSelectedHistory] = useState<HistoryEntry | undefined>()

  // Filter for ShipRun jobs
  const shipRunJobs = jobs.filter((j) => j.type === 'ShipRun')
  const activeJobs = shipRunJobs.filter((j) => j.status === 'Running' || j.status === 'Queued')
  const selectedJob = selectedJobId ? shipRunJobs.find((j) => j.id === selectedJobId) : undefined

  // React to viewingJobId from store (navigation from Jobs panel)
  useEffect(() => {
    if (viewingJobId) {
      const job = shipRunJobs.find((j) => j.id === viewingJobId)
      if (job) {
        setSelectedJobId(viewingJobId)
      }
      // Clear the viewing job after handling
      setViewingJob(null)
    }
  }, [viewingJobId, shipRunJobs, setViewingJob])

  // Load history on mount
  useEffect(() => {
    loadHistory()
  }, [])

  const loadHistory = async () => {
    setHistoryLoading(true)
    try {
      const entries = await api.history()
      setHistory(entries)
    } catch (e) {
      console.error('Failed to load history:', e)
    } finally {
      setHistoryLoading(false)
    }
  }

  const handleCancel = async (jobId: string) => {
    try {
      await cancelJob(jobId)
    } catch (e) {
      console.error('Failed to cancel job:', e)
    }
  }

  return (
    <div className="h-full p-1">
      <TileSplit direction="vertical" sizes={[50, 50]}>
        {/* Top: Active runs */}
        <TileSplit direction="horizontal" sizes={[40, 60]}>
          <Tile>
            <ActiveRuns
              jobs={activeJobs}
              selectedId={selectedJobId}
              onSelect={(job) => setSelectedJobId(job.id)}
            />
          </Tile>
          <Tile>
            <RunDetail job={selectedJob} onCancel={handleCancel} />
          </Tile>
        </TileSplit>

        {/* Bottom: History */}
        <TileSplit direction="horizontal" sizes={[50, 50]}>
          <Tile>
            <HistoryList
              entries={history}
              loading={historyLoading}
              selectedIssue={selectedHistory?.issue}
              onSelect={setSelectedHistory}
              onRefresh={loadHistory}
            />
          </Tile>
          <Tile>
            <HistoryDetail entry={selectedHistory} />
          </Tile>
        </TileSplit>
      </TileSplit>
    </div>
  )
}

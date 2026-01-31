import { useEffect, useState } from 'react'
import { ActivityButton } from '@/components/ui/activity-button'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import {
  useJobsStore,
  selectActiveJobCount,
  selectAggregateProgress,
  useRunningJobs,
  useQueuedJobs,
  useCompletedJobs,
} from '@/stores/jobs'
import type { Job } from '@/lib/api'
import { Loader2, CheckCircle2, XCircle, Circle, X, ChevronRight, ServerCog } from 'lucide-react'

function formatElapsed(startedAt?: string): string {
  if (!startedAt) return ''
  const start = new Date(startedAt).getTime()
  const now = Date.now()
  const seconds = Math.floor((now - start) / 1000)

  if (seconds < 60) return `${seconds}s`
  const minutes = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${minutes}:${secs.toString().padStart(2, '0')}`
}

function ProgressRing({ progress, size = 24, strokeWidth = 2.5 }: { progress: number | null; size?: number; strokeWidth?: number }) {
  const radius = (size - strokeWidth) / 2
  const circumference = radius * 2 * Math.PI
  const offset = circumference - ((progress ?? 0) / 100) * circumference

  return (
    <svg width={size} height={size} className="transform -rotate-90">
      {/* Background circle */}
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke="currentColor"
        strokeWidth={strokeWidth}
        className="text-muted-foreground/20"
      />
      {/* Progress circle */}
      {progress !== null && (
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          strokeWidth={strokeWidth}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          className="text-primary transition-all duration-300"
        />
      )}
    </svg>
  )
}

function JobItem({ job, onCancel, onView }: { job: Job; onCancel: () => void; onView: () => void }) {
  const [elapsed, setElapsed] = useState(formatElapsed(job.startedAt))

  // Update elapsed time every second for running jobs
  useEffect(() => {
    if (job.status !== 'Running') return
    const interval = setInterval(() => {
      setElapsed(formatElapsed(job.startedAt))
    }, 1000)
    return () => clearInterval(interval)
  }, [job.status, job.startedAt])

  const statusIcon = {
    Queued: <Circle className="h-3 w-3 text-muted-foreground" />,
    Running: <Loader2 className="h-3 w-3 text-blue-400 animate-spin" />,
    Completed: <CheckCircle2 className="h-3 w-3 text-green-400" />,
    Failed: <XCircle className="h-3 w-3 text-red-400" />,
    Cancelled: <XCircle className="h-3 w-3 text-muted-foreground" />,
  }

  const canCancel = job.status === 'Running' || job.status === 'Queued'
  const canView = job.status === 'Completed' || job.status === 'Failed'

  return (
    <div className="px-3 py-2 hover:bg-secondary/50 transition-colors">
      <div className="flex items-center gap-2">
        {statusIcon[job.status]}
        <span className="flex-1 text-sm font-medium truncate">{job.title}</span>
        <span className="text-xs text-muted-foreground">
          {job.status === 'Running' && elapsed}
          {job.status === 'Queued' && 'queued'}
          {job.status === 'Completed' && 'done'}
          {job.status === 'Failed' && 'failed'}
          {job.status === 'Cancelled' && 'cancelled'}
        </span>
      </div>

      {/* Stage info for running jobs */}
      {job.status === 'Running' && job.stage && (
        <div className="text-xs text-muted-foreground mt-1 ml-5 truncate">
          {job.stage}
        </div>
      )}

      {/* Error preview for failed jobs */}
      {job.status === 'Failed' && job.error && (
        <div className="text-xs text-red-400 mt-1 ml-5 truncate">
          {job.error}
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center gap-1 mt-1 ml-5">
        {canCancel && (
          <Button
            variant="ghost"
            size="sm"
            className="h-6 px-2 text-xs text-muted-foreground hover:text-foreground"
            onClick={(e) => {
              e.stopPropagation()
              onCancel()
            }}
          >
            <X className="h-3 w-3 mr-1" />
            Cancel
          </Button>
        )}
        {canView && (
          <Button
            variant="ghost"
            size="sm"
            className="h-6 px-2 text-xs"
            onClick={(e) => {
              e.stopPropagation()
              onView()
            }}
          >
            View
            <ChevronRight className="h-3 w-3 ml-1" />
          </Button>
        )}
      </div>
    </div>
  )
}

interface JobsIndicatorProps {
  onViewResult?: (job: Job) => void
}

export function JobsIndicator({ onViewResult }: JobsIndicatorProps) {
  const [open, setOpen] = useState(false)

  const jobs = useJobsStore((s) => s.jobs)
  const connect = useJobsStore((s) => s.connect)
  const disconnect = useJobsStore((s) => s.disconnect)
  const cancelJob = useJobsStore((s) => s.cancelJob)
  const clearCompleted = useJobsStore((s) => s.clearCompleted)

  const activeCount = useJobsStore(selectActiveJobCount)
  const progress = useJobsStore(selectAggregateProgress)
  const runningJobs = useRunningJobs()
  const queuedJobs = useQueuedJobs()
  const completedJobs = useCompletedJobs()

  // Connect to SSE on mount
  useEffect(() => {
    connect()
    return () => disconnect()
  }, [connect, disconnect])

  const hasJobs = jobs.length > 0
  const hasActive = activeCount > 0

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <ActivityButton
          active={hasJobs}
          pulsing={hasActive}
          count={hasJobs ? (activeCount > 0 ? activeCount : jobs.length) : undefined}
          title={hasActive ? `${activeCount} job(s) running` : 'Jobs'}
        >
          {/* Progress ring with icon */}
          <div className="relative flex items-center justify-center">
            <ProgressRing progress={hasActive ? progress : null} size={24} />
            <ServerCog className="absolute h-3 w-3" />
          </div>
        </ActivityButton>
      </PopoverTrigger>

      <PopoverContent align="end" className="w-80 p-0">
        <div className="flex items-center justify-between px-3 py-2 border-b">
          <span className="text-sm font-medium">Jobs</span>
          {completedJobs.length > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 px-2 text-xs text-muted-foreground"
              onClick={() => clearCompleted()}
            >
              Clear
            </Button>
          )}
        </div>

        <ScrollArea className="max-h-[400px]">
          {!hasJobs ? (
            <div className="py-8 text-center text-muted-foreground">
              <ServerCog className="h-8 w-8 mx-auto mb-2 opacity-50" />
              <p className="text-sm">No jobs</p>
              <p className="text-xs mt-1">Background tasks will appear here</p>
            </div>
          ) : (
            <div className="divide-y">
              {/* Running */}
              {runningJobs.map((job) => (
                <JobItem
                  key={job.id}
                  job={job}
                  onCancel={() => cancelJob(job.id)}
                  onView={() => onViewResult?.(job)}
                />
              ))}

              {/* Queued */}
              {queuedJobs.map((job) => (
                <JobItem
                  key={job.id}
                  job={job}
                  onCancel={() => cancelJob(job.id)}
                  onView={() => onViewResult?.(job)}
                />
              ))}

              {/* Completed */}
              {completedJobs.map((job) => (
                <JobItem
                  key={job.id}
                  job={job}
                  onCancel={() => cancelJob(job.id)}
                  onView={() => onViewResult?.(job)}
                />
              ))}
            </div>
          )}
        </ScrollArea>
      </PopoverContent>
    </Popover>
  )
}

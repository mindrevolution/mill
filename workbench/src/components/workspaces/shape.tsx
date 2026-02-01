import { useState, useEffect, useRef, useCallback, useMemo } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { FloatingActionBar } from '@/components/ui/floating-action-bar'
import { ActionDialog } from '@/components/ui/action-dialog'
import { DetailView } from '@/components/ui/detail-view'
import { Separator } from '@/components/ui/separator'
import { Terminal as TerminalComponent, type TerminalHandle } from '@/components/ui/terminal'
import { startSpecSession, type InteractiveSession } from '@/lib/runtime/pty'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger,
} from '@/components/ui/context-menu'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useSpecs } from '@/hooks/useApi'
import { api } from '@/lib/api'
import type { Draft, DraftDetail, Issue, IssueDetail, DraftValidationResponse } from '@/lib/api'
import { useJobsStore, getValidationResult } from '@/stores/jobs'
import {
  Plus,
  FileText,
  Bug,
  Shield,
  Wrench,
  Orbit,
  Loader2,
  AlertCircle,
  RefreshCw,
  Play,
  Terminal as TerminalIcon,
  ExternalLink,
  MoreHorizontal,
  Archive,
  Trash2,
  SearchCheck,
  X,
  Square,
} from 'lucide-react'

const typeIcons = {
  feature: FileText,
  bug: Bug,
  security: Shield,
  task: Wrench,
}

const typeColors = {
  feature: 'text-blue-400',
  bug: 'text-red-400',
  security: 'text-yellow-400',
  task: 'text-muted-foreground',
}

function IssueActionBar({
  issueNumber,
  onClose,
  onRefine,
}: {
  issueNumber: number
  onClose?: () => void
  onRefine?: () => void
}) {
  const issueUrl = `https://github.com/mindrevolution/mill/issues/${issueNumber}`

  return (
    <FloatingActionBar>
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0 hover:bg-primary hover:text-primary-foreground"
        title="Refine interactively"
        onClick={onRefine}
      >
        <TerminalIcon className="h-4 w-4" />
      </Button>
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0"
        title="Open in Browser"
        onClick={() => window.open(issueUrl, '_blank')}
      >
        <ExternalLink className="h-4 w-4" />
      </Button>
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0"
        title="Ship this"
        onClick={() => console.log('TODO: Ship this')}
      >
        <Play className="h-4 w-4" />
      </Button>
      <Separator orientation="vertical" className="h-4 mx-1" />
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0 text-destructive hover:bg-destructive hover:text-destructive-foreground"
        title="Close issue"
        onClick={onClose}
      >
        <Archive className="h-4 w-4" />
      </Button>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            size="sm"
            variant="ghost"
            className="h-8 w-8 p-0"
            title="More Actions"
          >
            <MoreHorizontal className="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onSelect={() => window.open(issueUrl, '_blank')}>
            Open in browser
          </DropdownMenuItem>
          <DropdownMenuItem
            onSelect={() => navigator.clipboard?.writeText(issueUrl)}
          >
            Copy issue link
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </FloatingActionBar>
  )
}

function DraftActionBar({
  onDelete,
  onValidate,
  onViewRelevance,
  onRefine,
  validating,
  hasRelevance,
}: {
  onDelete?: () => void
  onValidate?: () => void
  onViewRelevance?: () => void
  onRefine?: () => void
  validating?: boolean
  hasRelevance?: boolean
}) {
  return (
    <FloatingActionBar>
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0 hover:bg-primary hover:text-primary-foreground"
        title="Refine interactively"
        onClick={onRefine}
      >
        <TerminalIcon className="h-4 w-4" />
      </Button>
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0"
        title={hasRelevance ? 'View relevance' : 'Validate relevance'}
        onClick={hasRelevance ? onViewRelevance : onValidate}
        disabled={validating}
      >
        {validating ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <SearchCheck className="h-4 w-4" />
        )}
      </Button>
      <Separator orientation="vertical" className="h-4 mx-1" />
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0 text-destructive hover:bg-destructive hover:text-destructive-foreground"
        title="Delete draft"
        onClick={onDelete}
      >
        <Trash2 className="h-4 w-4" />
      </Button>
    </FloatingActionBar>
  )
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const days = Math.floor(diff / (1000 * 60 * 60 * 24))

  if (days === 0) return 'today'
  if (days === 1) return 'yesterday'
  if (days < 7) return `${days} days ago`
  if (days < 30) return `${Math.floor(days / 7)} weeks ago`
  return date.toLocaleDateString()
}

function SpecList({
  drafts,
  issues,
  loading,
  error,
  onRefresh,
  onSelectDraft,
  onSelectIssue,
  onNewSpec,
  selectedId,
}: {
  drafts: Draft[]
  issues: Issue[]
  loading: boolean
  error?: Error
  onRefresh: () => void
  onSelectDraft: (draft: Draft) => void
  onSelectIssue: (issue: Issue) => void
  onNewSpec: () => void
  selectedId?: string
}) {
  const [confirmDelete, setConfirmDelete] = useState<{
    kind: 'draft' | 'issue'
    id: string
    title: string
  } | null>(null)

  const handleDelete = () => {
    if (!confirmDelete) return
    // TODO: wire delete API when available
    console.log('Delete requested', confirmDelete)
    setConfirmDelete(null)
  }

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <span className="text-sm font-medium">Specs</span>
        <div className="flex items-center gap-1">
          <Button size="sm" variant="ghost" className="h-7 w-7 p-0" onClick={onRefresh}>
            <RefreshCw className={cn('h-3 w-3', loading && 'animate-spin')} />
          </Button>
          <Button size="sm" variant="ghost" className="h-7 px-2" onClick={onNewSpec}>
            <Plus className="h-3 w-3 mr-1" />
            New
          </Button>
        </div>
      </div>

      {error && (
        <div className="p-3 bg-destructive/10 border-b border-destructive/20 flex items-center gap-2 text-sm text-destructive">
          <AlertCircle className="h-4 w-4" />
          <span className="flex-1">Failed to load specs</span>
          <Button size="sm" variant="ghost" className="h-6 px-2 text-xs" onClick={onRefresh}>
            Retry
          </Button>
        </div>
      )}

      {loading && drafts.length === 0 && issues.length === 0 ? (
        <div className="flex-1 flex items-center justify-center text-muted-foreground">
          <Loader2 className="h-5 w-5 animate-spin" />
        </div>
      ) : (
        <div className="flex-1 min-h-0">
          <Command className="h-full">
            <div className="px-2 pt-2">
              <CommandInput placeholder="Search..." />
            </div>
            <CommandList className="h-full">
              <ScrollArea className="h-full">
                <div className="p-2">
                  {/* Drafts */}
              {drafts.length > 0 && (
                    <CommandGroup
                      heading="Drafts"
                      className="space-y-1.5 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-semibold [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-[0.2em] [&_[cmdk-group-heading]]:text-muted-foreground/70"
                    >
                      {drafts.map((draft) => {
                        const Icon = typeIcons[draft.type] || FileText
                        const color = typeColors[draft.type] || 'text-muted-foreground'
                        return (
                          <ContextMenu key={draft.id}>
                            <ContextMenuTrigger asChild>
                              <CommandItem
                                value={draft.title}
                                onSelect={() => onSelectDraft(draft)}
                                onMouseDown={() => onSelectDraft(draft)}
                                className={cn(
                                  'group relative flex-col items-start gap-1.5 rounded-md border border-border/40 bg-muted/20 px-2.5 py-2 text-sm transition-colors hover:bg-muted/40 aria-selected:bg-secondary/70 aria-selected:text-foreground aria-selected:border-primary/30',
                                  "before:absolute before:left-0 before:top-2 before:bottom-2 before:w-0.5 before:rounded-full before:bg-transparent aria-selected:before:bg-[#ffcc00]",
                                  selectedId === draft.id &&
                                    "bg-secondary/70 text-foreground border-primary/30 before:bg-[#ffcc00]"
                                )}
                              >
                                <div className="flex items-center gap-2 w-full">
                                  <Icon className={cn('h-3.5 w-3.5', color)} />
                                  <span className="text-sm font-medium leading-tight tracking-tight truncate flex-1">
                                    {draft.title}
                                  </span>
                                  {draft.status === 'ready' && (
                                    <Badge
                                      variant="secondary"
                                      className="rounded-full border border-emerald-500/30 bg-emerald-500/15 px-2 py-0 text-[10px] font-semibold uppercase tracking-wide text-emerald-300"
                                    >
                                      ready
                                    </Badge>
                                  )}
                                </div>
                                <div className="flex items-center gap-2 text-[11px] text-muted-foreground/80 w-full">
                                  <span>{formatDate(draft.updatedAt)}</span>
                                  {draft.persona && <span>· {draft.persona}</span>}
                                </div>
                              </CommandItem>
                            </ContextMenuTrigger>
                            <ContextMenuContent>
                              <ContextMenuItem onSelect={() => onSelectDraft(draft)}>
                                Open
                              </ContextMenuItem>
                              <ContextMenuSeparator />
                              <ContextMenuItem
                                onSelect={() =>
                                  setConfirmDelete({
                                    kind: 'draft',
                                    id: draft.id,
                                    title: draft.title,
                                  })
                                }
                              >
                                Delete draft
                              </ContextMenuItem>
                            </ContextMenuContent>
                          </ContextMenu>
                        )
                      })}
                    </CommandGroup>
                  )}

                  {/* Issues */}
              {issues.length > 0 && (
                    <CommandGroup
                      heading="Published Specs"
                      className="mt-2 space-y-1.5 [&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-semibold [&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-[0.2em] [&_[cmdk-group-heading]]:text-muted-foreground/70"
                    >
                      {issues.map((issue) => {
                        const Icon = typeIcons[issue.type] || FileText
                        const color = typeColors[issue.type] || 'text-muted-foreground'
                        return (
                          <CommandItem
                            key={issue.number}
                            value={`${issue.number} ${issue.title}`}
                            onSelect={() => onSelectIssue(issue)}
                            className={cn(
                              'group relative flex-col items-start gap-1.5 rounded-md border border-border/40 bg-muted/20 px-2.5 py-2 text-sm transition-colors hover:bg-muted/40 aria-selected:bg-secondary/70 aria-selected:text-foreground aria-selected:border-primary/30',
                              "before:absolute before:left-0 before:top-2 before:bottom-2 before:w-0.5 before:rounded-full before:bg-transparent aria-selected:before:bg-[#ffcc00]",
                              selectedId === `issue-${issue.number}` &&
                                "bg-secondary/70 text-foreground border-primary/30 before:bg-[#ffcc00]"
                            )}
                          >
                            <div className="flex items-center gap-2 w-full">
                              <span className="text-sm leading-tight tracking-tight truncate flex-1">
                                {issue.title}
                              </span>
                              <div className="flex items-center gap-2 shrink-0">
                                <span className="inline-flex items-center gap-1.5 rounded-md border border-border/60 bg-background/70 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
                                  <Icon className={cn('h-3 w-3', color)} />
                                  <span className="font-mono">#{issue.number}</span>
                                </span>
                                <Badge
                                  variant="secondary"
                                  className="rounded-full border border-border/50 bg-background/70 px-2 py-0 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground"
                                >
                                  {issue.status}
                                </Badge>
                              </div>
                            </div>
                            <div className="flex items-center gap-2 text-[11px] text-muted-foreground/80 w-full">
                              <span>{formatDate(issue.createdAt)}</span>
                            </div>
                          </CommandItem>
                        )
                      })}
                    </CommandGroup>
                  )}

                  <CommandEmpty>
                    <div className="py-8 text-center text-muted-foreground">
                      <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p className="text-sm">No matching specs</p>
                      <p className="text-xs mt-1">Try a different search</p>
                    </div>
                  </CommandEmpty>
                </div>
              </ScrollArea>
            </CommandList>
          </Command>
        </div>
      )}

      <Dialog open={confirmDelete !== null} onOpenChange={(open) => !open && setConfirmDelete(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete {confirmDelete?.kind}</DialogTitle>
            <DialogDescription>
              This will remove &quot;{confirmDelete?.title}&quot; from the list.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDelete(null)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function NewSpecInput({ onStart }: { onStart: (prompt: string) => void }) {
  const [input, setInput] = useState('')

  const handleStart = () => {
    if (!input.trim()) return
    onStart(input.trim())
  }

  return (
    <div className="h-full flex flex-col">
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="text-center space-y-4 max-w-md">
          <Orbit className="h-12 w-12 mx-auto text-muted-foreground/50" />
          <div>
            <h3 className="text-lg font-medium">New Spec</h3>
            <p className="text-sm text-muted-foreground mt-1">
              Describe what you want to build and start an interactive session to shape your spec.
            </p>
          </div>
        </div>
      </div>
      <div className="p-3 border-t">
        <div className="flex gap-2">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && !e.shiftKey && handleStart()}
            placeholder="Describe what you want to build..."
            className="flex-1"
          />
          <Button onClick={handleStart} size="icon" disabled={!input.trim()} title="Start interactive session">
            <TerminalIcon className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}

const verdictColors = {
  current: 'text-green-400 bg-green-500/15',
  review: 'text-yellow-400 bg-yellow-500/15',
  discard: 'text-red-400 bg-red-500/15',
}

const severityColors = {
  info: 'text-blue-400',
  warning: 'text-yellow-400',
  critical: 'text-red-400',
}

function ValidationResultDialog({
  open,
  onOpenChange,
  result,
  onDelete,
  onRefine,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  result: DraftValidationResponse | null
  onDelete?: () => void
  onRefine?: () => void
}) {
  if (!result) return null

  return (
    <ActionDialog
      open={open}
      onOpenChange={onOpenChange}
      className="max-w-2xl"
      title={
        <span className="flex items-center gap-3">
          <span>Relevance Validation</span>
          <Badge className={cn('text-sm', verdictColors[result.verdict])}>
            {result.score}/10 — {result.verdict}
          </Badge>
        </span>
      }
      description={result.summary}
      actions={
        <>
          <Button
            size="sm"
            variant="ghost"
            className="h-8 w-8 p-0 hover:bg-primary hover:text-primary-foreground"
            title="Refine interactively"
            onClick={() => {
              onRefine?.()
              onOpenChange(false)
            }}
          >
            <TerminalIcon className="h-4 w-4" />
          </Button>
          <Separator orientation="vertical" className="h-4 mx-1" />
          <Button
            size="sm"
            variant="ghost"
            className="h-8 w-8 p-0 text-destructive hover:bg-destructive hover:text-destructive-foreground"
            title="Delete cached result"
            onClick={() => {
              onDelete?.()
              onOpenChange(false)
            }}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
          <Separator orientation="vertical" className="h-4 mx-1" />
          <Button
            size="sm"
            variant="ghost"
            className="h-8 w-8 p-0"
            title="Close"
            onClick={() => onOpenChange(false)}
          >
            <X className="h-4 w-4" />
          </Button>
        </>
      }
    >
      {result.findings.length > 0 && (
        <div className="mb-4">
          <h4 className="text-sm font-medium mb-3">Findings</h4>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {result.findings.map((finding, i) => (
              <div key={i} className="text-sm border rounded-lg p-3 space-y-1">
                <div className="flex items-center gap-2">
                  <Badge variant="outline" className={severityColors[finding.severity]}>
                    {finding.severity}
                  </Badge>
                  <span className="font-medium">{finding.category}</span>
                </div>
                <p className="text-muted-foreground">{finding.reference}</p>
                <div className="text-xs space-y-1 pt-1">
                  <p><span className="text-muted-foreground">Expected:</span> {finding.expected}</p>
                  <p><span className="text-muted-foreground">Actual:</span> {finding.actual}</p>
                  <p><span className="text-muted-foreground">Impact:</span> {finding.impact}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="border-t pt-3">
        <h4 className="text-sm font-medium mb-1">Recommendation</h4>
        <p className="text-sm text-muted-foreground">{result.recommendation}</p>
      </div>
    </ActionDialog>
  )
}

function SpecPreview({
  draftDetail,
  issueDetail,
  loading,
  onRelevanceChanged,
  onRefineInteractively,
  onIssueClosed,
  onIssueCloseStart,
  onIssueCloseFailed,
}: {
  draftDetail?: DraftDetail
  issueDetail?: IssueDetail
  loading?: boolean
  onRelevanceChanged?: () => void
  onRefineInteractively?: (draftId?: string, issueNumber?: number) => void
  onIssueClosed?: (issueNumber: number) => void
  onIssueCloseStart?: (issueNumber: number) => void
  onIssueCloseFailed?: (issueNumber: number) => void
}) {
  const [validationResult, setValidationResult] = useState<DraftValidationResponse | null>(null)
  const [showValidationDialog, setShowValidationDialog] = useState(false)
  const [pendingValidationJobId, setPendingValidationJobId] = useState<string | null>(null)
  const [confirmCloseIssue, setConfirmCloseIssue] = useState<IssueDetail | null>(null)
  const [closingIssue, setClosingIssue] = useState(false)
  const [closeIssueError, setCloseIssueError] = useState<string | null>(null)

  const jobs = useJobsStore((s) => s.jobs)
  const createJob = useJobsStore((s) => s.createJob)

  // Watch for validation job completion
  useEffect(() => {
    if (!pendingValidationJobId) return

    const job = jobs.find((j) => j.id === pendingValidationJobId)
    if (!job) return

    if (job.status === 'Completed') {
      const result = getValidationResult(job)
      if (result) {
        setValidationResult(result)
        setShowValidationDialog(true)
        // Notify parent that relevance was created
        onRelevanceChanged?.()
      }
      setPendingValidationJobId(null)
    } else if (job.status === 'Failed' || job.status === 'Cancelled') {
      setPendingValidationJobId(null)
    }
  }, [jobs, pendingValidationJobId, onRelevanceChanged])

  const handleValidate = async (draftId: string) => {
    try {
      const jobId = await createJob({
        type: 'DraftValidation',
        params: { draftId },
        sourceWorkspace: 'shape',
      })
      setPendingValidationJobId(jobId)
    } catch (e) {
      console.error('Failed to create validation job:', e)
    }
  }

  const handleViewRelevance = async (draftId: string) => {
    try {
      const result = await api.spec.getRelevance(draftId)
      setValidationResult(result)
      setShowValidationDialog(true)
    } catch (e) {
      console.error('Failed to load relevance:', e)
    }
  }

  const handleDeleteRelevance = async (draftId: string) => {
    try {
      await api.spec.deleteRelevance(draftId)
      setValidationResult(null)
      onRelevanceChanged?.()
    } catch (e) {
      console.error('Failed to delete relevance:', e)
    }
  }

  const handleCloseIssue = async (issue: IssueDetail) => {
    onIssueCloseStart?.(issue.number)
    setCloseIssueError(null)
    setClosingIssue(true)
    try {
      await api.spec.closeIssue(issue.number)
      onIssueClosed?.(issue.number)
      setConfirmCloseIssue(null)
    } catch (e) {
      console.error('Failed to close issue:', e)
      onIssueCloseFailed?.(issue.number)
      setCloseIssueError('Failed to close issue. Check permissions and try again.')
    } finally {
      setClosingIssue(false)
    }
  }

  // Check if there's a running validation job for the current draft
  const currentDraftId = draftDetail?.id
  const validatingJob = jobs.find(
    (j) =>
      j.type === 'DraftValidation' &&
      j.params.draftId === currentDraftId &&
      (j.status === 'Running' || j.status === 'Queued')
  )
  const validating = !!validatingJob

  // Show loading state
  if (loading) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <Loader2 className="h-5 w-5 animate-spin" />
      </div>
    )
  }

  // Unified view for both draft and issue
  const spec = issueDetail || draftDetail
  if (spec) {
    const isDraft = !issueDetail
    const Icon = typeIcons[spec.type] || FileText
    const color = typeColors[spec.type] || 'text-muted-foreground'
    const bodyHtml = 'bodyHtml' in spec ? spec.bodyHtml : undefined

    return (
      <>
        <DetailView
          header={
            <>
              <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
                <Icon className={cn('h-3 w-3', color)} />
                {isDraft ? (
                  <Badge variant="outline">Draft</Badge>
                ) : (
                  <span>#{(spec as IssueDetail).number}</span>
                )}
                <Badge variant="secondary">{spec.type}</Badge>
                {!isDraft && (
                  <Badge variant={(spec as IssueDetail).status === 'open' ? 'default' : 'secondary'}>
                    {(spec as IssueDetail).status}
                  </Badge>
                )}
                {spec.persona && <span>· {spec.persona}</span>}
              </div>
              <h2 className="text-lg font-semibold">
                {isDraft ? spec.title : (
                  <span dangerouslySetInnerHTML={{ __html: (spec as IssueDetail).titleHtml }} />
                )}
              </h2>
              {/* Labels (issues only) */}
              {!isDraft && (spec as IssueDetail).labels.length > 0 && (
                <div className="flex flex-wrap gap-1 mt-2">
                  {(spec as IssueDetail).labels.map((label) => (
                    <Badge key={label} variant="outline" className="text-xs">
                      {label}
                    </Badge>
                  ))}
                </div>
              )}
            </>
          }
          actions={
            isDraft ? (
              <DraftActionBar
                onDelete={() => console.log('TODO: Delete draft', (spec as DraftDetail).id)}
                onValidate={() => handleValidate((spec as DraftDetail).id)}
                onViewRelevance={() => handleViewRelevance((spec as DraftDetail).id)}
                onRefine={() => onRefineInteractively?.((spec as DraftDetail).id)}
                validating={validating}
                hasRelevance={(spec as DraftDetail).hasRelevance}
              />
            ) : (
              <IssueActionBar
                issueNumber={(spec as IssueDetail).number}
                onClose={() => setConfirmCloseIssue(spec as IssueDetail)}
                onRefine={() => onRefineInteractively?.(undefined, (spec as IssueDetail).number)}
              />
            )
          }
        >
          <div className="space-y-4">
            {/* Body */}
            {bodyHtml ? (
              <div
                className="prose prose-sm prose-invert max-w-none"
                dangerouslySetInnerHTML={{ __html: bodyHtml }}
              />
            ) : (
              <div className="text-sm text-muted-foreground italic">
                No description provided
              </div>
            )}

            {/* Meta */}
            <div className="text-xs text-muted-foreground pt-2 border-t">
              {isDraft
                ? `Updated ${formatDate((spec as DraftDetail).updatedAt)}`
                : `Created ${formatDate((spec as IssueDetail).createdAt)}`}
            </div>
          </div>
        </DetailView>

        <ValidationResultDialog
          open={showValidationDialog}
          onOpenChange={setShowValidationDialog}
          result={validationResult}
          onDelete={draftDetail ? () => handleDeleteRelevance(draftDetail.id) : undefined}
          onRefine={draftDetail ? () => onRefineInteractively?.(draftDetail.id) : undefined}
        />

        <Dialog
          open={confirmCloseIssue !== null}
          onOpenChange={(open) => !open && !closingIssue && setConfirmCloseIssue(null)}
        >
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Close issue #{confirmCloseIssue?.number}</DialogTitle>
              <DialogDescription>
                This will close the issue on GitHub and remove it from the open specs list.
                You can reopen it later.
              </DialogDescription>
              {closeIssueError && (
                <p className="text-sm text-destructive mt-2">{closeIssueError}</p>
              )}
            </DialogHeader>
            <DialogFooter>
              <Button
                variant="outline"
                onClick={() => setConfirmCloseIssue(null)}
                disabled={closingIssue}
              >
                Cancel
              </Button>
              <Button
                variant="destructive"
                onClick={() => confirmCloseIssue && handleCloseIssue(confirmCloseIssue)}
                disabled={closingIssue}
              >
                {closingIssue ? (
                  <>
                    <Loader2 className="h-3 w-3 animate-spin" />
                    Closing…
                  </>
                ) : (
                  'Close issue'
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </>
    )
  }

  // Empty state
  return (
    <div className="h-full flex items-center justify-center text-muted-foreground">
      <div className="text-center">
        <Orbit className="h-8 w-8 mx-auto mb-2 opacity-50" />
        <p className="text-sm">Spec preview will appear here</p>
        <p className="text-xs mt-1">Start a conversation to build your spec</p>
      </div>
    </div>
  )
}

interface InteractiveTerminalProps {
  draftId?: string
  issueNumber?: number
  initialPrompt?: string
  onClose: () => void
}

function InteractiveTerminal({ draftId, issueNumber, initialPrompt, onClose }: InteractiveTerminalProps) {
  const terminalRef = useRef<TerminalHandle>(null)
  const sessionRef = useRef<InteractiveSession | null>(null)
  const [exited, setExited] = useState(false)
  const [exitCode, setExitCode] = useState<number | null>(null)
  // Use ref for synchronous guard (state updates are async and cause race conditions)
  const sessionStartingRef = useRef(false)
  const cleanupRef = useRef<{
    unsubOutput?: () => void
    unsubExit?: () => void
  }>({})

  // Start session when terminal reports its initial dimensions
  const startSession = useCallback(async (cols: number, rows: number) => {
    // Synchronous guard using ref to prevent race conditions
    if (sessionStartingRef.current || sessionRef.current) return
    sessionStartingRef.current = true

    try {
      // Use high-level spec session API - backend handles all command building
      const session = await startSpecSession({
        mode: issueNumber ? 'refine' : 'draft',
        issueNumber,
        draftId,
        initialPrompt,
        cols,
        rows,
      })

      sessionRef.current = session

      // Wire up output
      cleanupRef.current.unsubOutput = session.onOutput((data) => {
        terminalRef.current?.write(data)
      })

      // Wire up exit
      cleanupRef.current.unsubExit = session.onExit((code) => {
        setExited(true)
        setExitCode(code)
      })

      // Focus terminal
      terminalRef.current?.focus()
    } catch (error) {
      console.error('[PTY] Failed to start session:', error)
      setExited(true)
      setExitCode(-1)
    }
  }, [issueNumber, draftId, initialPrompt])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      cleanupRef.current.unsubOutput?.()
      cleanupRef.current.unsubExit?.()
      sessionRef.current?.kill()
    }
  }, [])


  // Handle user input
  const handleData = useCallback((data: string) => {
    sessionRef.current?.write(data)
  }, [])

  // Track last sent dimensions to avoid duplicate resizes
  const lastDimensionsRef = useRef<{ cols: number; rows: number } | null>(null)

  // Handle resize - also triggers initial session start
  const handleResize = useCallback((cols: number, rows: number) => {
    // Skip if dimensions haven't changed
    const last = lastDimensionsRef.current
    if (last && last.cols === cols && last.rows === rows) {
      return
    }
    lastDimensionsRef.current = { cols, rows }

    if (!sessionRef.current) {
      // First resize callback = terminal is ready, start session
      startSession(cols, rows)
    } else {
      // Resize PTY - Claude Code will re-render at new size
      sessionRef.current.resize(cols, rows)
    }
  }, [startSession])

  return (
    <div className="h-full flex flex-col bg-[#09090b]">
      {/* Header bar */}
      <div className="flex items-center justify-between px-3 py-2 border-b bg-card">
        <div className="flex items-center gap-2 text-sm">
          <TerminalIcon className="h-4 w-4 text-muted-foreground" />
          <span className="font-medium">Interactive Session</span>
          {draftId && <Badge variant="outline" className="text-xs">Draft</Badge>}
          {issueNumber && <Badge variant="outline" className="text-xs">#{issueNumber}</Badge>}
          {!draftId && !issueNumber && <Badge variant="outline" className="text-xs">New Spec</Badge>}
        </div>
        <div className="flex items-center gap-1">
          {exited && (
            <Badge
              variant={exitCode === 0 ? 'default' : 'destructive'}
              className="text-xs mr-2"
            >
              Exited ({exitCode})
            </Badge>
          )}
          <Button
            size="sm"
            variant="ghost"
            className="h-7 w-7 p-0"
            title="Stop session"
            onClick={() => {
              sessionRef.current?.kill()
              onClose()
            }}
          >
            <Square className="h-3 w-3" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="h-7 w-7 p-0"
            title="Close"
            onClick={onClose}
          >
            <X className="h-3 w-3" />
          </Button>
        </div>
      </div>

      {/* Terminal */}
      <div className="flex-1 min-h-0">
        <TerminalComponent
          ref={terminalRef}
          onData={handleData}
          onResize={handleResize}
          className="h-full"
        />
      </div>
    </div>
  )
}

export function ShapeWorkspace() {
  const { drafts, issues, loading, error, refetch } = useSpecs()
  const [selectedDraft, setSelectedDraft] = useState<Draft | undefined>()
  const [selectedId, setSelectedId] = useState<string | undefined>()
  const [draftDetail, setDraftDetail] = useState<DraftDetail | undefined>()
  const [issueDetail, setIssueDetail] = useState<IssueDetail | undefined>()
  const [loadingSpec, setLoadingSpec] = useState(false)
  const [hiddenIssues, setHiddenIssues] = useState<Set<number>>(() => new Set())

  // Interactive terminal session state
  const [interactiveSession, setInteractiveSession] = useState<{
    draftId?: string
    issueNumber?: number
    initialPrompt?: string
  } | null>(null)

  // Watch for job result viewing
  const viewingJobId = useJobsStore((s) => s.viewingJobId)
  const setViewingJob = useJobsStore((s) => s.setViewingJob)
  const jobs = useJobsStore((s) => s.jobs)

  useEffect(() => {
    if (!viewingJobId) return
    const job = jobs.find((j) => j.id === viewingJobId)
    if (!job || job.type !== 'DraftValidation') return

    const draftId = job.params?.draftId as string | undefined
    if (!draftId) return

    // Find the draft and select it
    const draft = drafts.find((d) => d.id === draftId)
    if (draft) {
      // Select the draft (this will load details and show validation result)
      setSelectedDraft(draft)
      setSelectedId(draft.id)
      setDraftDetail(undefined)
      setIssueDetail(undefined)
      setLoadingSpec(true)

      api.spec.draft(draft.id).then((detail) => {
        setDraftDetail(detail)
        setLoadingSpec(false)
      }).catch(() => {
        setLoadingSpec(false)
      })
    }

    // Clear viewing state
    setViewingJob(null)
  }, [viewingJobId, jobs, drafts, setViewingJob])

  useEffect(() => {
    if (hiddenIssues.size === 0) return
    setHiddenIssues((prev) => {
      const next = new Set<number>()
      prev.forEach((id) => {
        if (issues.some((issue) => issue.number === id)) {
          next.add(id)
        }
      })
      return next
    })
  }, [issues, hiddenIssues.size])

  const visibleIssues = useMemo(
    () => issues.filter((issue) => !hiddenIssues.has(issue.number)),
    [issues, hiddenIssues]
  )

  const handleSelectDraft = async (draft: Draft) => {
    setSelectedDraft(draft)
    setDraftDetail(undefined)
    setIssueDetail(undefined)
    setSelectedId(draft.id)
    setLoadingSpec(true)

    try {
      const detail = await api.spec.draft(draft.id)
      setDraftDetail(detail)
    } catch (e) {
      console.error('Failed to fetch draft details:', e)
    } finally {
      setLoadingSpec(false)
    }
  }

  const handleSelectIssue = async (issue: Issue) => {
    setSelectedDraft(undefined)
    setDraftDetail(undefined)
    setIssueDetail(undefined)
    setSelectedId(`issue-${issue.number}`)
    setLoadingSpec(true)

    try {
      const detail = await api.spec.issue(issue.number)
      setIssueDetail(detail)
    } catch (e) {
      console.error('Failed to fetch issue details:', e)
    } finally {
      setLoadingSpec(false)
    }
  }

  const handleIssueClosed = (issueNumber: number) => {
    setHiddenIssues((prev) => new Set(prev).add(issueNumber))
    if (selectedId === `issue-${issueNumber}`) {
      setSelectedId(undefined)
      setIssueDetail(undefined)
      setSelectedDraft(undefined)
      setDraftDetail(undefined)
      setLoadingSpec(false)
    }
    refetch(true)
  }

  const handleIssueCloseStart = (issueNumber: number) => {
    setHiddenIssues((prev) => new Set(prev).add(issueNumber))
  }

  const handleIssueCloseFailed = (issueNumber: number) => {
    setHiddenIssues((prev) => {
      const next = new Set(prev)
      next.delete(issueNumber)
      return next
    })
    refetch(true)
  }

  const handleNewSpec = () => {
    // Clear selection and close any active session to show NewSpecInput
    setSelectedDraft(undefined)
    setSelectedId(undefined)
    setDraftDetail(undefined)
    setIssueDetail(undefined)
    setInteractiveSession(null)
  }

  // Refresh draft detail to update hasRelevance flag
  const handleRelevanceChanged = async () => {
    if (!draftDetail) return
    try {
      const detail = await api.spec.draft(draftDetail.id)
      setDraftDetail(detail)
    } catch (e) {
      console.error('Failed to refresh draft details:', e)
    }
  }

  // Start interactive refinement session
  const handleRefineInteractively = (draftId?: string, issueNumber?: number) => {
    setInteractiveSession({ draftId, issueNumber })
  }

  // Start new spec with initial prompt
  const handleStartNewSpec = (initialPrompt: string) => {
    setInteractiveSession({ initialPrompt })
  }

  // Close interactive session
  const handleCloseInteractiveSession = () => {
    setInteractiveSession(null)
  }

  return (
    <div className="h-full p-1">
      <TileSplit direction="horizontal" sizes={[25, 40, 35]}>
        <Tile>
          <SpecList
            drafts={drafts}
            issues={visibleIssues}
            loading={loading}
            error={error}
            onRefresh={() => refetch(true)}
            onSelectDraft={handleSelectDraft}
            onSelectIssue={handleSelectIssue}
            onNewSpec={handleNewSpec}
            selectedId={selectedId}
          />
        </Tile>
        <Tile>
          {interactiveSession ? (
            <InteractiveTerminal
              draftId={interactiveSession.draftId}
              issueNumber={interactiveSession.issueNumber}
              initialPrompt={interactiveSession.initialPrompt}
              onClose={handleCloseInteractiveSession}
            />
          ) : (
            <NewSpecInput onStart={handleStartNewSpec} />
          )}
        </Tile>
        <Tile>
          <SpecPreview
            draftDetail={draftDetail}
            issueDetail={issueDetail}
            onRelevanceChanged={handleRelevanceChanged}
            onRefineInteractively={handleRefineInteractively}
            onIssueClosed={handleIssueClosed}
            onIssueCloseStart={handleIssueCloseStart}
            onIssueCloseFailed={handleIssueCloseFailed}
            loading={loadingSpec}
          />
        </Tile>
      </TileSplit>
    </div>
  )
}

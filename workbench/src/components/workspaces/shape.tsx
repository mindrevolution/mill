import { useState, useEffect } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { FloatingActionBar } from '@/components/ui/floating-action-bar'
import { DetailView } from '@/components/ui/detail-view'
import { Separator } from '@/components/ui/separator'
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
import type { ChatMessage } from '@/types'
import {
  Plus,
  Send,
  FileText,
  Bug,
  Shield,
  Wrench,
  Orbit,
  Loader2,
  AlertCircle,
  RefreshCw,
  Play,
  Terminal,
  ExternalLink,
  MoreHorizontal,
  Trash2,
  SearchCheck,
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

function IssueActionBar({ issueNumber, onDelete }: { issueNumber: number; onDelete?: () => void }) {
  const issueUrl = `https://github.com/mindrevolution/mill/issues/${issueNumber}`

  return (
    <FloatingActionBar>
      <Button
        size="sm"
        variant="ghost"
        className="h-8 w-8 p-0 hover:bg-primary hover:text-primary-foreground"
        title="Refine interactively"
        onClick={() => console.log('TODO: Refine interactively')}
      >
        <Terminal className="h-4 w-4" />
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
        onClick={onDelete}
      >
        <Trash2 className="h-4 w-4" />
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
  validating,
  hasRelevance,
}: {
  onDelete?: () => void
  onValidate?: () => void
  onViewRelevance?: () => void
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
        onClick={() => console.log('TODO: Refine interactively')}
      >
        <Terminal className="h-4 w-4" />
      </Button>
      {hasRelevance ? (
        <Button
          size="sm"
          variant="ghost"
          className="h-8 w-8 p-0 text-green-400"
          title="View relevance"
          onClick={onViewRelevance}
        >
          <SearchCheck className="h-4 w-4" />
        </Button>
      ) : (
        <Button
          size="sm"
          variant="ghost"
          className="h-8 w-8 p-0"
          title="Validate relevance"
          onClick={onValidate}
          disabled={validating}
        >
          {validating ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <SearchCheck className="h-4 w-4" />
          )}
        </Button>
      )}
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
                    <CommandGroup heading="Drafts">
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
                                  selectedId === draft.id && 'bg-secondary text-foreground'
                                )}
                              >
                                <div className="flex items-center gap-2 w-full">
                                  <Icon className={cn('h-3 w-3', color)} />
                                  <span className="text-sm font-medium truncate flex-1">{draft.title}</span>
                                  {draft.status === 'ready' && (
                                    <Badge variant="default" className="text-[10px] px-1 py-0">ready</Badge>
                                  )}
                                </div>
                                <div className="flex items-center gap-2 text-xs text-muted-foreground mt-1 w-full">
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
                    <CommandGroup heading="Published Specs">
                      {issues.map((issue) => {
                        const Icon = typeIcons[issue.type] || FileText
                        const color = typeColors[issue.type] || 'text-muted-foreground'
                        return (
                          <CommandItem
                            key={issue.number}
                            value={`${issue.number} ${issue.title}`}
                            onSelect={() => onSelectIssue(issue)}
                            className={cn(
                              'flex-col items-start gap-1',
                              selectedId === `issue-${issue.number}` && 'bg-secondary text-foreground'
                            )}
                          >
                            <div className="flex items-center gap-2 w-full">
                              <Icon className={cn('h-3 w-3', color)} />
                              <span className="text-xs text-muted-foreground">#{issue.number}</span>
                              <span className="text-sm truncate flex-1">{issue.title}</span>
                              <Badge variant="secondary" className="text-[10px] px-1 py-0">{issue.status}</Badge>
                            </div>
                            <div className="flex items-center gap-2 text-xs text-muted-foreground w-full">
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

function SpecChat({ draft, sessionId }: { draft?: Draft; sessionId?: string }) {
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: '1',
      role: 'assistant',
      content: draft
        ? `I see you're working on "${draft.title}". What would you like to refine?`
        : 'What would you like to build? Describe the feature, bug, or task.',
      timestamp: new Date().toISOString(),
    },
  ])
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)

  const sendMessage = async () => {
    if (!input.trim() || sending) return

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: input,
      timestamp: new Date().toISOString(),
    }

    setMessages((prev) => [...prev, userMessage])
    setInput('')
    setSending(true)

    try {
      const response = await api.spec.message(sessionId || 'new', input)
      setMessages((prev) => [
        ...prev,
        {
          id: (Date.now() + 1).toString(),
          role: 'assistant',
          content: response.content,
          timestamp: new Date().toISOString(),
        },
      ])
    } catch {
      setMessages((prev) => [
        ...prev,
        {
          id: (Date.now() + 1).toString(),
          role: 'assistant',
          content: 'Sorry, I encountered an error. Please try again.',
          timestamp: new Date().toISOString(),
        },
      ])
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="h-full flex flex-col">
      <ScrollArea className="flex-1 p-4">
        <div className="space-y-4">
          {messages.map((msg) => (
            <div
              key={msg.id}
              className={cn(
                'flex',
                msg.role === 'user' ? 'justify-end' : 'justify-start'
              )}
            >
              <div
                className={cn(
                  'max-w-[80%] rounded-lg px-3 py-2 text-sm',
                  msg.role === 'user'
                    ? 'bg-primary text-primary-foreground'
                    : 'bg-card border'
                )}
              >
                {msg.content}
              </div>
            </div>
          ))}
          {sending && (
            <div className="flex justify-start">
              <div className="bg-card border rounded-lg px-3 py-2">
                <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
              </div>
            </div>
          )}
        </div>
      </ScrollArea>
      <div className="p-3 border-t">
        <div className="flex gap-2">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && !e.shiftKey && sendMessage()}
            placeholder="Describe what you want to build..."
            className="flex-1"
            disabled={sending}
          />
          <Button onClick={sendMessage} size="icon" disabled={sending}>
            {sending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
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
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  result: DraftValidationResponse | null
}) {
  if (!result) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg max-h-[80vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-3">
            <span>Relevance Validation</span>
            <Badge className={cn('text-sm', verdictColors[result.verdict])}>
              {result.score}/10 — {result.verdict}
            </Badge>
          </DialogTitle>
          <DialogDescription>{result.summary}</DialogDescription>
        </DialogHeader>

        <ScrollArea className="flex-1 -mx-6 px-6">
          {result.findings.length > 0 && (
            <div className="space-y-3 mb-4">
              <h4 className="text-sm font-medium">Findings</h4>
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
          )}

          <div className="border-t pt-3">
            <h4 className="text-sm font-medium mb-1">Recommendation</h4>
            <p className="text-sm text-muted-foreground">{result.recommendation}</p>
          </div>
        </ScrollArea>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function SpecPreview({
  draftDetail,
  issueDetail,
  loading,
  onRelevanceChanged,
}: {
  draftDetail?: DraftDetail
  issueDetail?: IssueDetail
  loading?: boolean
  onRelevanceChanged?: () => void
}) {
  const [validationResult, setValidationResult] = useState<DraftValidationResponse | null>(null)
  const [showValidationDialog, setShowValidationDialog] = useState(false)
  const [pendingValidationJobId, setPendingValidationJobId] = useState<string | null>(null)

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
                validating={validating}
                hasRelevance={(spec as DraftDetail).hasRelevance}
              />
            ) : (
              <IssueActionBar
                issueNumber={(spec as IssueDetail).number}
                onDelete={() => console.log('TODO: Close issue', (spec as IssueDetail).number)}
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
        />
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

export function ShapeWorkspace() {
  const { drafts, issues, loading, error, refetch } = useSpecs()
  const [selectedDraft, setSelectedDraft] = useState<Draft | undefined>()
  const [selectedId, setSelectedId] = useState<string | undefined>()
  const [sessionId, setSessionId] = useState<string | undefined>()
  const [draftDetail, setDraftDetail] = useState<DraftDetail | undefined>()
  const [issueDetail, setIssueDetail] = useState<IssueDetail | undefined>()
  const [loadingSpec, setLoadingSpec] = useState(false)

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

  const handleNewSpec = async () => {
    try {
      const session = await api.spec.start()
      setSessionId(session.id)
      setSelectedDraft(undefined)
      setSelectedId(undefined)
    } catch {
      // TODO: Show error toast
      console.error('Failed to start spec session')
    }
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

  return (
    <div className="h-full p-1">
      <TileSplit direction="horizontal" sizes={[25, 40, 35]}>
        <Tile>
          <SpecList
            drafts={drafts}
            issues={issues}
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
          <SpecChat draft={selectedDraft} sessionId={sessionId} />
        </Tile>
        <Tile>
          <SpecPreview
            draftDetail={draftDetail}
            issueDetail={issueDetail}
            onRelevanceChanged={handleRelevanceChanged}
            loading={loadingSpec}
          />
        </Tile>
      </TileSplit>
    </div>
  )
}

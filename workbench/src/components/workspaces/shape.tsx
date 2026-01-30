import { useState } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { useSpecs } from '@/hooks/useApi'
import { api } from '@/lib/api'
import type { Draft, Issue } from '@/lib/api'
import type { ChatMessage } from '@/types'
import {
  Plus,
  Send,
  FileText,
  Bug,
  Shield,
  Wrench,
  ChevronRight,
  Sparkles,
  Loader2,
  AlertCircle,
  RotateCcw,
  RefreshCw,
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
        <ScrollArea className="flex-1">
          <div className="p-2">
            {/* Drafts */}
            {drafts.length > 0 && (
              <div className="mb-4">
                <div className="px-2 py-1 text-xs text-muted-foreground font-medium">Drafts</div>
                {drafts.map((draft) => {
                  const Icon = typeIcons[draft.type] || FileText
                  const color = typeColors[draft.type] || 'text-muted-foreground'
                  return (
                    <button
                      key={draft.id}
                      onClick={() => onSelectDraft(draft)}
                      className={cn(
                        'w-full text-left p-2 rounded-md transition-colors',
                        selectedId === draft.id ? 'bg-secondary' : 'hover:bg-secondary/50'
                      )}
                    >
                      <div className="flex items-center gap-2 mb-1">
                        <Icon className={cn('h-3 w-3', color)} />
                        <span className="text-sm font-medium truncate flex-1">{draft.title}</span>
                        {draft.status === 'ready' && (
                          <Badge variant="default" className="text-[10px] px-1 py-0">ready</Badge>
                        )}
                      </div>
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <span>{formatDate(draft.updatedAt)}</span>
                        {draft.persona && <span>· {draft.persona}</span>}
                      </div>
                    </button>
                  )
                })}
              </div>
            )}

            {/* Issues */}
            {issues.length > 0 && (
              <div>
                <div className="px-2 py-1 text-xs text-muted-foreground font-medium">Issues</div>
                {issues.map((issue) => {
                  const Icon = typeIcons[issue.type] || FileText
                  const color = typeColors[issue.type] || 'text-muted-foreground'
                  return (
                    <button
                      key={issue.number}
                      onClick={() => onSelectIssue(issue)}
                      className={cn(
                        'w-full text-left p-2 rounded-md transition-colors',
                        selectedId === `issue-${issue.number}` ? 'bg-secondary' : 'hover:bg-secondary/50'
                      )}
                    >
                      <div className="flex items-center gap-2 mb-1">
                        <Icon className={cn('h-3 w-3', color)} />
                        <span className="text-xs text-muted-foreground">#{issue.number}</span>
                        <span className="text-sm truncate flex-1">{issue.title}</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <span>{formatDate(issue.createdAt)}</span>
                        <Badge variant="secondary" className="text-[10px] px-1 py-0">{issue.status}</Badge>
                      </div>
                    </button>
                  )
                })}
              </div>
            )}

            {/* Empty state */}
            {drafts.length === 0 && issues.length === 0 && !loading && (
              <div className="py-8 text-center text-muted-foreground">
                <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">No specs yet</p>
                <p className="text-xs mt-1">Create one to get started</p>
              </div>
            )}
          </div>
        </ScrollArea>
      )}
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

function SpecPreview({ draft }: { draft?: Draft }) {
  if (!draft) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Sparkles className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">Spec preview will appear here</p>
          <p className="text-xs mt-1">Start a conversation to build your spec</p>
        </div>
      </div>
    )
  }

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-4">
        {/* Header */}
        <div>
          <div className="flex items-center gap-2 text-xs text-muted-foreground mb-1">
            <Badge variant="secondary">{draft.type}</Badge>
            {draft.persona && <span>· {draft.persona}</span>}
          </div>
          <h2 className="text-lg font-semibold">{draft.title}</h2>
        </div>

        {/* Sections */}
        <div className="space-y-3">
          <section className="p-3 rounded-md bg-card border hover:border-primary/50 cursor-pointer transition-colors">
            <div className="flex items-center justify-between mb-1">
              <h3 className="text-sm font-medium">Summary</h3>
              <ChevronRight className="h-3 w-3 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground">
              Add the ability to sync data while offline and reconcile when connection is restored.
            </p>
          </section>

          <section className="p-3 rounded-md bg-card border hover:border-primary/50 cursor-pointer transition-colors">
            <div className="flex items-center justify-between mb-1">
              <h3 className="text-sm font-medium">Acceptance Criteria</h3>
              <ChevronRight className="h-3 w-3 text-muted-foreground" />
            </div>
            <ul className="text-sm text-muted-foreground space-y-1">
              <li>• Changes made offline are queued locally</li>
              <li>• Queue syncs automatically when online</li>
              <li>• Conflicts are surfaced to user</li>
            </ul>
          </section>

          <section className="p-3 rounded-md bg-card border hover:border-primary/50 cursor-pointer transition-colors">
            <div className="flex items-center justify-between mb-1">
              <h3 className="text-sm font-medium">Verification</h3>
              <ChevronRight className="h-3 w-3 text-muted-foreground" />
            </div>
            <p className="text-sm text-muted-foreground">
              Integration tests for offline queue, unit tests for conflict resolution.
            </p>
          </section>
        </div>

        {/* Actions - auto-save, so only "Create Issue" and "Revert" */}
        <div className="flex gap-2 pt-2">
          <Button className="flex-1">Create Issue</Button>
          <Button variant="outline" size="icon" title="Revert changes">
            <RotateCcw className="h-4 w-4" />
          </Button>
        </div>

        {/* Auto-save indicator */}
        <div className="text-xs text-muted-foreground text-center">
          Auto-saved · Last change {formatDate(draft.updatedAt)}
        </div>
      </div>
    </ScrollArea>
  )
}

export function ShapeWorkspace() {
  const { drafts, issues, loading, error, refetch } = useSpecs()
  const [selectedDraft, setSelectedDraft] = useState<Draft | undefined>()
  const [selectedId, setSelectedId] = useState<string | undefined>()
  const [focusedTile, setFocusedTile] = useState<'list' | 'chat' | 'preview'>('list')
  const [sessionId, setSessionId] = useState<string | undefined>()

  const handleSelectDraft = (draft: Draft) => {
    setSelectedDraft(draft)
    setSelectedId(draft.id)
    setFocusedTile('chat')
  }

  const handleSelectIssue = (issue: Issue) => {
    setSelectedDraft(undefined)
    setSelectedId(`issue-${issue.number}`)
    setFocusedTile('chat')
  }

  const handleNewSpec = async () => {
    try {
      const session = await api.spec.start()
      setSessionId(session.id)
      setSelectedDraft(undefined)
      setSelectedId(undefined)
      setFocusedTile('chat')
    } catch {
      // TODO: Show error toast
      console.error('Failed to start spec session')
    }
  }

  return (
    <div className="h-full p-1">
      <TileSplit direction="horizontal" sizes={[25, 40, 35]}>
        <Tile
          title="Specs"
          focused={focusedTile === 'list'}
          onFocus={() => setFocusedTile('list')}
        >
          <SpecList
            drafts={drafts}
            issues={issues}
            loading={loading}
            error={error}
            onRefresh={refetch}
            onSelectDraft={handleSelectDraft}
            onSelectIssue={handleSelectIssue}
            onNewSpec={handleNewSpec}
            selectedId={selectedId}
          />
        </Tile>
        <Tile
          title="Chat"
          focused={focusedTile === 'chat'}
          onFocus={() => setFocusedTile('chat')}
        >
          <SpecChat draft={selectedDraft} sessionId={sessionId} />
        </Tile>
        <Tile
          title="Preview"
          focused={focusedTile === 'preview'}
          onFocus={() => setFocusedTile('preview')}
        >
          <SpecPreview draft={selectedDraft} />
        </Tile>
      </TileSplit>
    </div>
  )
}

import { useState } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import type { Draft, Issue, ChatMessage } from '@/types'
import {
  Plus,
  Send,
  FileText,
  Bug,
  Shield,
  Wrench,
  ChevronRight,
  Sparkles,
} from 'lucide-react'

// Mock data
const mockDrafts: Draft[] = [
  { id: '1', slug: 'offline-sync', title: 'Add offline sync support', type: 'feature', status: 'draft', updatedAt: '2 hours ago' },
  { id: '2', slug: 'login-bug', title: 'Fix login timeout issue', type: 'bug', status: 'ready', updatedAt: '1 day ago', persona: 'mobile-user' },
]

const mockIssues: Issue[] = [
  { number: 42, title: 'Implement dark mode toggle', type: 'feature', status: 'open', createdAt: '3 days ago' },
  { number: 41, title: 'API rate limiting', type: 'security', status: 'in-progress', createdAt: '5 days ago' },
  { number: 40, title: 'Refactor auth module', type: 'task', status: 'open', createdAt: '1 week ago' },
]

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

function SpecList({
  onSelectDraft,
  onSelectIssue,
  selectedId,
}: {
  onSelectDraft: (draft: Draft) => void
  onSelectIssue: (issue: Issue) => void
  selectedId?: string
}) {
  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <span className="text-sm font-medium">Specs</span>
        <Button size="sm" variant="ghost" className="h-7 px-2">
          <Plus className="h-3 w-3 mr-1" />
          New
        </Button>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2">
          {/* Drafts */}
          {mockDrafts.length > 0 && (
            <div className="mb-4">
              <div className="px-2 py-1 text-xs text-muted-foreground font-medium">Drafts</div>
              {mockDrafts.map((draft) => {
                const Icon = typeIcons[draft.type]
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
                      <Icon className={cn('h-3 w-3', typeColors[draft.type])} />
                      <span className="text-sm font-medium truncate flex-1">{draft.title}</span>
                      {draft.status === 'ready' && (
                        <Badge variant="default" className="text-[10px] px-1 py-0">ready</Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span>{draft.updatedAt}</span>
                      {draft.persona && <span>· {draft.persona}</span>}
                    </div>
                  </button>
                )
              })}
            </div>
          )}

          {/* Issues */}
          <div>
            <div className="px-2 py-1 text-xs text-muted-foreground font-medium">Issues</div>
            {mockIssues.map((issue) => {
              const Icon = typeIcons[issue.type]
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
                    <Icon className={cn('h-3 w-3', typeColors[issue.type])} />
                    <span className="text-xs text-muted-foreground">#{issue.number}</span>
                    <span className="text-sm truncate flex-1">{issue.title}</span>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span>{issue.createdAt}</span>
                    <Badge variant="secondary" className="text-[10px] px-1 py-0">{issue.status}</Badge>
                  </div>
                </button>
              )
            })}
          </div>
        </div>
      </ScrollArea>
    </div>
  )
}

function SpecChat({ draft }: { draft?: Draft }) {
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

  const sendMessage = () => {
    if (!input.trim()) return
    setMessages([
      ...messages,
      { id: Date.now().toString(), role: 'user', content: input, timestamp: new Date().toISOString() },
    ])
    setInput('')
    // TODO: Send to API
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
        </div>
      </ScrollArea>
      <div className="p-3 border-t">
        <div className="flex gap-2">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && sendMessage()}
            placeholder="Describe what you want to build..."
            className="flex-1"
          />
          <Button onClick={sendMessage} size="icon">
            <Send className="h-4 w-4" />
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

        {/* Actions */}
        <div className="flex gap-2 pt-2">
          <Button className="flex-1">Create Issue</Button>
          <Button variant="outline">Save Draft</Button>
        </div>
      </div>
    </ScrollArea>
  )
}

export function ShapeWorkspace() {
  const [selectedDraft, setSelectedDraft] = useState<Draft | undefined>(mockDrafts[0])
  const [selectedId, setSelectedId] = useState<string | undefined>('1')
  const [focusedTile, setFocusedTile] = useState<'list' | 'chat' | 'preview'>('list')

  const handleSelectDraft = (draft: Draft) => {
    setSelectedDraft(draft)
    setSelectedId(draft.id)
  }

  const handleSelectIssue = (issue: Issue) => {
    setSelectedDraft(undefined)
    setSelectedId(`issue-${issue.number}`)
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
            onSelectDraft={handleSelectDraft}
            onSelectIssue={handleSelectIssue}
            selectedId={selectedId}
          />
        </Tile>
        <Tile
          title="Chat"
          focused={focusedTile === 'chat'}
          onFocus={() => setFocusedTile('chat')}
        >
          <SpecChat draft={selectedDraft} />
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

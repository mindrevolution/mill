import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Textarea } from '@/components/ui/textarea'
import { Input } from '@/components/ui/input'
import { FloatingActionBar } from '@/components/ui/floating-action-bar'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import type { Brief, BriefStage } from '@/types'
import { Zap, Trash2, ArrowRight, Save, Terminal } from 'lucide-react'
import { formatDaysRemaining, calculateDecay } from './utils'
import { StagePills } from './stage-pills'

interface BriefDetailProps {
  brief?: Brief
  content?: string
  onUpdate: (id: string, data: { stage?: BriefStage; intent?: string; content?: string }) => void
  onDrop: (id: string, essence: string) => void
  onPromote: (id: string, type: string) => void
}

const intentTypes = [
  { value: 'feature', label: 'Feature' },
  { value: 'bug', label: 'Bug' },
  { value: 'security', label: 'Security' },
  { value: 'task', label: 'Task' },
]

export function BriefDetail({ brief, content, onUpdate, onDrop, onPromote }: BriefDetailProps) {
  const [editedIntent, setEditedIntent] = useState('')
  const [editedContent, setEditedContent] = useState('')
  const [dropDialogOpen, setDropDialogOpen] = useState(false)
  const [dropEssence, setDropEssence] = useState('')
  const [promoteDialogOpen, setPromoteDialogOpen] = useState(false)
  const [promoteType, setPromoteType] = useState('feature')

  // Reset local state when brief changes
  useEffect(() => {
    if (brief) {
      setEditedIntent(brief.intent)
      setEditedContent(content || '')
    }
  }, [brief?.id, content])

  if (!brief) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Zap className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">Select a brief to view details</p>
          <p className="text-xs mt-1 opacity-70">Or create a new one</p>
        </div>
      </div>
    )
  }

  const decay = calculateDecay(brief.createdAt)
  const daysLeft = formatDaysRemaining(brief.createdAt)

  const handleStageChange = (stage: BriefStage) => {
    onUpdate(brief.id, { stage })
  }

  const handleSave = () => {
    onUpdate(brief.id, {
      intent: editedIntent !== brief.intent ? editedIntent : undefined,
      content: editedContent !== content ? editedContent : undefined,
    })
  }

  const handleDrop = () => {
    if (dropEssence.trim()) {
      onDrop(brief.id, dropEssence.trim())
      setDropDialogOpen(false)
      setDropEssence('')
    }
  }

  const handlePromote = () => {
    onPromote(brief.id, promoteType)
    setPromoteDialogOpen(false)
  }

  const hasChanges = editedIntent !== brief.intent || editedContent !== (content || '')

  return (
    <div className="h-full relative">
      <ScrollArea className="h-full">
        <div className="p-4 space-y-4">
          {/* Header */}
          <div className="flex items-start justify-between">
            <div>
              <h2 className="text-lg font-semibold">{brief.title}</h2>
              <span
                className="text-xs text-muted-foreground"
                style={{ opacity: decay }}
              >
                {daysLeft}
              </span>
            </div>
          </div>

          {/* Stage pills */}
          <StagePills value={brief.stage} onChange={handleStageChange} />

          <Separator />

          {/* Intent */}
          <div className="space-y-2">
            <label className="text-sm text-muted-foreground">Intent</label>
            <Textarea
              value={editedIntent || brief.intent}
              onChange={(e) => setEditedIntent(e.target.value)}
              placeholder="What problem does this solve? Who benefits?"
              className="min-h-[80px] resize-none"
            />
          </div>

          {/* Notes/Content */}
          <div className="space-y-2">
            <label className="text-sm text-muted-foreground">Notes</label>
            <Textarea
              value={editedContent || content || ''}
              onChange={(e) => setEditedContent(e.target.value)}
              placeholder="Add context, research, or ideas..."
              className="min-h-[150px] resize-none font-mono text-sm"
            />
          </div>

          {/* Metadata */}
          {(brief.persona || brief.concepts?.length) && (
            <>
              <Separator />
              <div className="space-y-2 text-sm">
                {brief.persona && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Persona</span>
                    <Badge variant="outline">{brief.persona}</Badge>
                  </div>
                )}
                {brief.concepts?.length && (
                  <div className="flex justify-between items-start">
                    <span className="text-muted-foreground">Concepts</span>
                    <div className="flex flex-wrap gap-1 justify-end">
                      {brief.concepts.map((c) => (
                        <Badge key={c} variant="outline" className="text-xs">
                          {c}
                        </Badge>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </>
          )}

          {/* Save + Promote actions */}
          {(hasChanges || brief.stage === 'ready') && (
            <>
              <Separator />
              <div className="flex gap-2">
                {hasChanges && (
                  <Button onClick={handleSave} className="flex-1">
                    <Save className="h-4 w-4 mr-1" />
                    Save
                  </Button>
                )}
                {brief.stage === 'ready' && (
                  <Button
                    variant="default"
                    className="flex-1 bg-emerald-600 hover:bg-emerald-700"
                    onClick={() => setPromoteDialogOpen(true)}
                  >
                    <ArrowRight className="h-4 w-4 mr-1" />
                    Promote
                  </Button>
                )}
              </div>
            </>
          )}

          {/* Spacer for floating bar */}
          <div className="h-12" />
        </div>
      </ScrollArea>

      {/* Floating Action Bar */}
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
        <Separator orientation="vertical" className="h-4 mx-1" />
        <Button
          size="sm"
          variant="ghost"
          className="h-8 w-8 p-0 text-destructive hover:bg-destructive hover:text-destructive-foreground"
          title="Drop Brief"
          onClick={() => setDropDialogOpen(true)}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </FloatingActionBar>

      {/* Drop Dialog */}
      <Dialog open={dropDialogOpen} onOpenChange={setDropDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Drop Brief</DialogTitle>
            <DialogDescription>
              Capture the essence of why this brief is being dropped.
              This helps preserve learnings for the future.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Input
              placeholder="Single sentence: why drop this?"
              value={dropEssence}
              onChange={(e) => setDropEssence(e.target.value)}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDropDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDrop}
              disabled={!dropEssence.trim()}
            >
              Drop Brief
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Promote Dialog */}
      <Dialog open={promoteDialogOpen} onOpenChange={setPromoteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Promote to Shape</DialogTitle>
            <DialogDescription>
              This brief will become a draft in the Shape workspace.
              Select the intent type.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Select value={promoteType} onValueChange={setPromoteType}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {intentTypes.map((type) => (
                  <SelectItem key={type.value} value={type.value}>
                    {type.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setPromoteDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handlePromote}>Promote</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

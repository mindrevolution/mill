import { useState, useCallback, useEffect } from 'react'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import type { Brief, BriefStage } from '@/types'
import { useBriefs, useDroppedBriefs } from '@/hooks/useApi'
import { api } from '@/lib/api'
import { BriefList } from './brief-list'
import { BriefDetail } from './brief-detail'
import { DroppedTray } from './dropped-tray'

export function BriefWorkspace() {
  const { data: briefs, refetch: refetchBriefs } = useBriefs()
  const { data: dropped, refetch: refetchDropped } = useDroppedBriefs()
  const [selectedBrief, setSelectedBrief] = useState<Brief | undefined>()
  const [briefContent, setBriefContent] = useState<string>('')
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [newTitle, setNewTitle] = useState('')
  const [newIntent, setNewIntent] = useState('')

  // Load brief content when selection changes
  useEffect(() => {
    if (selectedBrief) {
      api.brief.get(selectedBrief.id).then((detail) => {
        setBriefContent(detail.content)
      })
    } else {
      setBriefContent('')
    }
  }, [selectedBrief?.id])

  // Update selected brief when briefs list changes
  useEffect(() => {
    if (selectedBrief && briefs) {
      const updated = briefs.find((b) => b.id === selectedBrief.id)
      if (updated) {
        setSelectedBrief(updated)
      } else {
        // Brief was deleted (dropped or promoted)
        setSelectedBrief(undefined)
      }
    }
  }, [briefs])

  const handleSelect = useCallback((brief: Brief) => {
    setSelectedBrief(brief)
  }, [])

  const handleCreate = useCallback(async () => {
    if (newTitle.trim() && newIntent.trim()) {
      const brief = await api.brief.create(newTitle.trim(), newIntent.trim())
      await refetchBriefs()
      setSelectedBrief(brief)
      setCreateDialogOpen(false)
      setNewTitle('')
      setNewIntent('')
    }
  }, [newTitle, newIntent, refetchBriefs])

  const handleUpdate = useCallback(async (
    id: string,
    data: { stage?: BriefStage; intent?: string; content?: string }
  ) => {
    await api.brief.update(id, data)
    await refetchBriefs()
    if (data.content !== undefined) {
      setBriefContent(data.content)
    }
  }, [refetchBriefs])

  const handleDrop = useCallback(async (id: string, essence: string) => {
    await api.brief.drop(id, essence)
    await refetchBriefs()
    await refetchDropped()
    setSelectedBrief(undefined)
  }, [refetchBriefs, refetchDropped])

  const handlePromote = useCallback(async (id: string, type: string) => {
    await api.brief.promote(id, type)
    await refetchBriefs()
    setSelectedBrief(undefined)
  }, [refetchBriefs])

  return (
    <>
      <div className="h-full p-1">
        <TileSplit direction="horizontal" sizes={[30, 45, 25]}>
          <Tile>
            <BriefList
              briefs={briefs ?? []}
              selectedId={selectedBrief?.id}
              onSelect={handleSelect}
              onCreate={() => setCreateDialogOpen(true)}
            />
          </Tile>
          <Tile>
            <BriefDetail
              brief={selectedBrief}
              content={briefContent}
              onUpdate={handleUpdate}
              onDrop={handleDrop}
              onPromote={handlePromote}
            />
          </Tile>
          <Tile>
            <DroppedTray dropped={dropped ?? []} />
          </Tile>
        </TileSplit>
      </div>

      {/* Create Brief Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New Brief</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm text-muted-foreground">Title</label>
              <Input
                placeholder="What's the idea?"
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm text-muted-foreground">Intent</label>
              <Textarea
                placeholder="What problem does this solve? Who benefits?"
                value={newIntent}
                onChange={(e) => setNewIntent(e.target.value)}
                className="min-h-[100px] resize-none"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreate}
              disabled={!newTitle.trim() || !newIntent.trim()}
            >
              Create Brief
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

// Re-export components
export { BriefList } from './brief-list'
export { BriefDetail } from './brief-detail'
export { DroppedTray } from './dropped-tray'
export { StagePills } from './stage-pills'
export { stageConfig, calculateDecay, daysRemaining, formatDaysRemaining } from './utils'

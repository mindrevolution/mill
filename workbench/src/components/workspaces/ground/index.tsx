import { useState, useEffect, useCallback } from 'react'
import { Sparkles, Loader2 } from 'lucide-react'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { LibraryTree } from './library-tree'
import { ItemDetail } from './item-detail'
import { ObservationsTray } from './observations-tray'
import { KickstartWizard } from './kickstart-wizard'
import { mockObservations } from './utils'
import { api, type GroundStatus, type KnowledgeItem } from '@/lib/api'

export function GroundWorkspace() {
  const [selectedItem, setSelectedItem] = useState<KnowledgeItem | undefined>()
  const [groundStatus, setGroundStatus] = useState<GroundStatus | null>(null)
  const [items, setItems] = useState<KnowledgeItem[]>([])
  const [loading, setLoading] = useState(true)
  const [wizardOpen, setWizardOpen] = useState(false)

  // Load ground status and items
  const loadGround = useCallback(async () => {
    setLoading(true)
    try {
      const [status, personas, standards, concepts, design] = await Promise.all([
        api.ground.status(),
        api.ground.items('personas'),
        api.ground.items('standards'),
        api.ground.items('concepts'),
        api.ground.items('design'),
      ])
      setGroundStatus(status)
      setItems([...personas, ...standards, ...concepts, ...design])

      // Auto-open wizard if ground is empty
      if (status.isEmpty) {
        setWizardOpen(true)
      }
    } catch (err) {
      console.error('Failed to load ground data:', err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadGround()
  }, [loadGround])

  const handleWizardComplete = () => {
    // Reload ground data after kickstart completes
    loadGround()
  }

  if (loading && !groundStatus) {
    return (
      <div className="h-full flex items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="h-full p-1">
      <TileSplit direction="horizontal" sizes={[30, 40, 30]}>
        <Tile>
          <LibraryTree
            items={items}
            selectedId={selectedItem?.id}
            onSelect={setSelectedItem}
            headerActions={
              <Button
                size="sm"
                variant="ghost"
                className="h-7 px-2"
                onClick={() => setWizardOpen(true)}
              >
                <Sparkles className="h-3 w-3 mr-1" />
                Kickstart
              </Button>
            }
          />
        </Tile>
        <Tile>
          <ItemDetail item={selectedItem} />
        </Tile>
        <Tile>
          <ObservationsTray observations={mockObservations} />
        </Tile>
      </TileSplit>

      <KickstartWizard
        open={wizardOpen}
        onOpenChange={setWizardOpen}
        showConfirmation={groundStatus?.hasKickstart ?? false}
        onComplete={handleWizardComplete}
      />
    </div>
  )
}

// Re-export components for use elsewhere
export { LibraryTree } from './library-tree'
export { ItemDetail } from './item-detail'
export { ObservationsTray } from './observations-tray'
export { KickstartWizard } from './kickstart-wizard'
export { categoryMeta, categoryColors } from './utils'

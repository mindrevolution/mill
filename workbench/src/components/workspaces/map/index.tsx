import { useState } from 'react'
import { Tile, TileSplit } from '@/components/layout/tile'
import type { KnowledgeItem } from '@/types'
import { LibraryTree } from './library-tree'
import { ItemDetail } from './item-detail'
import { ObservationsTray } from './observations-tray'
import { mockLibrary, mockObservations } from './utils'

export function MapWorkspace() {
  const [selectedItem, setSelectedItem] = useState<KnowledgeItem | undefined>()

  return (
    <div className="h-full p-1">
      <TileSplit direction="horizontal" sizes={[30, 40, 30]}>
        <Tile>
          <LibraryTree
            items={mockLibrary}
            selectedId={selectedItem?.id}
            onSelect={setSelectedItem}
          />
        </Tile>
        <Tile>
          <ItemDetail item={selectedItem} />
        </Tile>
        <Tile>
          <ObservationsTray observations={mockObservations} />
        </Tile>
      </TileSplit>
    </div>
  )
}

// Re-export components for use elsewhere
export { LibraryTree } from './library-tree'
export { ItemDetail } from './item-detail'
export { ObservationsTray } from './observations-tray'
export { categoryMeta, categoryColors } from './utils'

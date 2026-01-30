import { useState } from 'react'
import { Tile, TileSplit } from '@/components/layout/tile'
import type { LibraryItem } from '@/types'
import { LibraryTree } from './library-tree'
import { ItemDetail } from './item-detail'
import { ObservationsTray } from './observations-tray'
import { mockLibrary, mockObservations } from './utils'

export function MapWorkspace() {
  const [selectedItem, setSelectedItem] = useState<LibraryItem | undefined>()
  const [focusedTile, setFocusedTile] = useState<'tree' | 'detail' | 'observations'>('tree')

  return (
    <div className="h-full p-1">
      <TileSplit direction="horizontal" sizes={[30, 40, 30]}>
        <Tile
          focused={focusedTile === 'tree'}
          onFocus={() => setFocusedTile('tree')}
        >
          <LibraryTree
            items={mockLibrary}
            selectedId={selectedItem?.id}
            onSelect={(item) => {
              setSelectedItem(item)
              setFocusedTile('detail')
            }}
          />
        </Tile>
        <Tile
          focused={focusedTile === 'detail'}
          onFocus={() => setFocusedTile('detail')}
        >
          <ItemDetail item={selectedItem} />
        </Tile>
        <Tile
          focused={focusedTile === 'observations'}
          onFocus={() => setFocusedTile('observations')}
        >
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

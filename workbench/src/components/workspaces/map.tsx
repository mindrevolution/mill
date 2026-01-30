import { useState } from 'react'
import { cn } from '@/lib/utils'
import { Tile, TileSplit } from '@/components/layout/tile'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import type { LibraryCategory, LibraryItem, Observation } from '@/types'
import {
  Users,
  BookOpen,
  Lightbulb,
  Palette,
  Plus,
  Check,
  X,
  Ban,
  ChevronRight,
  Sparkles,
  FolderOpen,
  File,
} from 'lucide-react'

const categoryMeta: Record<LibraryCategory, { label: string; icon: typeof Users; description: string }> = {
  personas: { label: 'Personas', icon: Users, description: 'Who you build for' },
  standards: { label: 'Standards', icon: BookOpen, description: 'How you build' },
  concepts: { label: 'Concepts', icon: Lightbulb, description: 'Domain vocabulary' },
  design: { label: 'Design', icon: Palette, description: 'Visual language' },
}

// Mock data
const mockLibrary: LibraryItem[] = [
  { id: '1', category: 'personas', name: 'Mobile User', description: 'Users primarily on mobile devices, often with spotty connectivity', file: 'mobile-user.md', createdAt: '2024-01-15', updatedAt: '2024-01-20' },
  { id: '2', category: 'personas', name: 'Power User', description: 'Technical users who want keyboard shortcuts and advanced features', file: 'power-user.md', createdAt: '2024-01-10', updatedAt: '2024-01-10' },
  { id: '3', category: 'standards', name: 'Async I/O', description: 'Use async/await for all I/O operations', file: 'async-io.md', createdAt: '2024-01-12', updatedAt: '2024-01-12' },
  { id: '4', category: 'standards', name: 'Error Handling', description: 'Return Result<T> instead of throwing exceptions', file: 'error-handling.md', createdAt: '2024-01-08', updatedAt: '2024-01-18' },
  { id: '5', category: 'concepts', name: 'Match', description: 'A game session between two players', file: 'match.md', createdAt: '2024-01-05', updatedAt: '2024-01-05' },
  { id: '6', category: 'concepts', name: 'Player', description: 'A user participating in matches', file: 'player.md', createdAt: '2024-01-05', updatedAt: '2024-01-05' },
  { id: '7', category: 'design', name: 'Color Tokens', description: 'Primary, secondary, and accent colors', file: 'colors.md', createdAt: '2024-01-02', updatedAt: '2024-01-15' },
]

const mockObservations: Observation[] = [
  { id: '1', category: 'personas', suggestion: 'Enterprise Admin — mentioned in 4 recent specs', source: 'spec:user-roles, spec:permissions', confidence: 0.85, createdAt: '2 hours ago' },
  { id: '2', category: 'standards', suggestion: 'Validation at boundaries — pattern in auth, API modules', source: 'run:41, run:38', confidence: 0.72, createdAt: '1 day ago' },
  { id: '3', category: 'concepts', suggestion: 'Tournament — appears in match-related specs', source: 'spec:tournaments, spec:rankings', confidence: 0.68, createdAt: '2 days ago' },
]

// Category colors
const categoryColors: Record<LibraryCategory, { badge: string; icon: string }> = {
  personas: { badge: 'bg-blue-500/15 text-blue-400', icon: 'text-blue-400' },
  standards: { badge: 'bg-emerald-500/15 text-emerald-400', icon: 'text-emerald-400' },
  concepts: { badge: 'bg-violet-500/15 text-violet-400', icon: 'text-violet-400' },
  design: { badge: 'bg-pink-500/15 text-pink-400', icon: 'text-pink-400' },
}

function LibraryTree({
  items,
  selectedId,
  onSelect,
}: {
  items: LibraryItem[]
  selectedId?: string
  onSelect: (item: LibraryItem) => void
}) {
  const [expandedCategories, setExpandedCategories] = useState<Set<LibraryCategory>>(
    new Set(['personas', 'standards', 'concepts', 'design'])
  )

  const toggleCategory = (category: LibraryCategory) => {
    const next = new Set(expandedCategories)
    if (next.has(category)) {
      next.delete(category)
    } else {
      next.add(category)
    }
    setExpandedCategories(next)
  }

  const itemsByCategory = items.reduce((acc, item) => {
    if (!acc[item.category]) acc[item.category] = []
    acc[item.category].push(item)
    return acc
  }, {} as Record<LibraryCategory, LibraryItem[]>)

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <span className="text-sm font-medium">Library</span>
        <Button size="sm" variant="ghost" className="h-7 px-2">
          <Plus className="h-3 w-3 mr-1" />
          Add
        </Button>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2">
          {(Object.keys(categoryMeta) as LibraryCategory[]).map((category) => {
            const meta = categoryMeta[category]
            const Icon = meta.icon
            const categoryItems = itemsByCategory[category] || []
            const isExpanded = expandedCategories.has(category)

            return (
              <div key={category} className="mb-2">
                <button
                  onClick={() => toggleCategory(category)}
                  className="w-full flex items-center gap-2 p-2 rounded-md hover:bg-secondary/50 transition-colors"
                >
                  <ChevronRight
                    className={cn(
                      'h-3 w-3 text-muted-foreground transition-transform',
                      isExpanded && 'rotate-90'
                    )}
                  />
                  <Icon className={cn('h-4 w-4', categoryColors[category].icon)} />
                  <span className="text-sm font-medium flex-1 text-left">{meta.label}</span>
                  <span className="text-xs text-muted-foreground">{categoryItems.length}</span>
                </button>

                {isExpanded && categoryItems.length > 0 && (
                  <div className="ml-5 mt-1 space-y-0.5">
                    {categoryItems.map((item) => (
                      <button
                        key={item.id}
                        onClick={() => onSelect(item)}
                        className={cn(
                          'w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-left transition-colors',
                          selectedId === item.id ? 'bg-secondary' : 'hover:bg-secondary/50'
                        )}
                      >
                        <File className="h-3 w-3 text-muted-foreground" />
                        <span className="text-sm truncate">{item.name}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

function ItemDetail({ item }: { item?: LibraryItem }) {
  if (!item) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <FolderOpen className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">Select an item to view details</p>
        </div>
      </div>
    )
  }

  const meta = categoryMeta[item.category]
  const Icon = meta.icon

  return (
    <ScrollArea className="h-full">
      <div className="p-4 space-y-4">
        <div>
          <div className="flex items-center gap-2 mb-2">
            <Icon className="h-4 w-4 text-muted-foreground" />
            <Badge variant="secondary">{meta.label}</Badge>
          </div>
          <h2 className="text-lg font-semibold">{item.name}</h2>
          <p className="text-sm text-muted-foreground mt-1">{item.description}</p>
        </div>

        <Separator />

        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">File</span>
            <span className="font-mono text-xs">{item.file}</span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">Created</span>
            <span>{item.createdAt}</span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">Updated</span>
            <span>{item.updatedAt}</span>
          </div>
        </div>

        <Separator />

        <div className="flex gap-2">
          <Button variant="outline" className="flex-1">Edit</Button>
          <Button variant="ghost" className="text-destructive">Delete</Button>
        </div>
      </div>
    </ScrollArea>
  )
}

// Confidence indicator component - thin progress bar
function ConfidenceBar({ confidence }: { confidence: number }) {
  return (
    <div
      className="w-8 h-1 rounded-full bg-muted-foreground/20 overflow-hidden"
      title={`${Math.round(confidence * 100)}% confidence`}
    >
      <div
        className="h-full bg-muted-foreground/60 rounded-full transition-all"
        style={{ width: `${confidence * 100}%` }}
      />
    </div>
  )
}

// Parse suggestion into title and detail
function parseSuggestion(suggestion: string): { title: string; detail: string } {
  const parts = suggestion.split(' — ')
  return {
    title: parts[0],
    detail: parts[1] || '',
  }
}

// Count sources from comma-separated string
function parseSourceCount(source: string): number {
  return source.split(',').length
}

function ObservationsTray({ observations }: { observations: Observation[] }) {
  if (observations.length === 0) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Sparkles className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">No new observations</p>
          <p className="text-xs mt-1">Mill will suggest additions as you work</p>
        </div>
      </div>
    )
  }

  // Sort by confidence (highest first)
  const sorted = [...observations].sort((a, b) => b.confidence - a.confidence)

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Lightbulb className="h-4 w-4 text-primary" />
          <span className="text-sm font-medium">Observations</span>
          <Badge>{observations.length}</Badge>
        </div>
        <Button size="sm" variant="ghost" className="h-7 text-xs">
          Review all
        </Button>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-2">
          {sorted.map((obs, index) => {
            const meta = categoryMeta[obs.category]
            const { title, detail } = parseSuggestion(obs.suggestion)
            const sourceCount = parseSourceCount(obs.source)
            const isTop = index === 0

            return (
              <div
                key={obs.id}
                className={cn(
                  'group p-3 rounded-lg border bg-card transition-colors',
                  isTop
                    ? 'border-primary/40 hover:border-primary/60'
                    : 'hover:border-muted-foreground/40'
                )}
              >
                {/* Header: Category badge + Confidence */}
                <div className="flex items-center justify-between mb-2">
                  <span
                    className={cn(
                      'text-[10px] font-medium px-1.5 py-0.5 rounded',
                      categoryColors[obs.category].badge
                    )}
                  >
                    {meta.label}
                  </span>
                  <ConfidenceBar confidence={obs.confidence} />
                </div>

                {/* Title */}
                <p className="text-sm font-medium">{title}</p>

                {/* Detail (if exists) */}
                {detail && (
                  <p className="text-xs text-muted-foreground mt-0.5">{detail}</p>
                )}

                {/* Footer: Source + Time + Actions */}
                <div className="flex items-center justify-between mt-3">
                  <p
                    className="text-[10px] text-muted-foreground"
                    title={obs.source}
                  >
                    {sourceCount} {sourceCount === 1 ? 'source' : 'sources'} · {obs.createdAt}
                  </p>

                  {/* Actions - subtle until hover */}
                  <div className="flex items-center gap-1 opacity-60 group-hover:opacity-100 transition-opacity">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-6 px-2 text-xs hover:bg-primary/20 hover:text-primary"
                    >
                      <Check className="h-3 w-3 mr-1" />
                      Add
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-6 w-6 p-0 text-muted-foreground"
                      title="Dismiss"
                    >
                      <X className="h-3 w-3" />
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-6 w-6 p-0 text-muted-foreground/60"
                      title="Never suggest again"
                    >
                      <Ban className="h-3 w-3" />
                    </Button>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

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

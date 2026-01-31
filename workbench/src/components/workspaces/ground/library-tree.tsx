import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from '@/components/ui/command'
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger,
} from '@/components/ui/context-menu'
import type { KnowledgeCategory, KnowledgeItem } from '@/types'
import { Plus, File } from 'lucide-react'
import { categoryMeta, categoryColors } from './utils'

interface LibraryTreeProps {
  items: KnowledgeItem[]
  selectedId?: string
  onSelect: (item: KnowledgeItem) => void
}

export function LibraryTree({ items, selectedId, onSelect }: LibraryTreeProps) {
  const itemsByCategory = items.reduce((acc, item) => {
    if (!acc[item.category]) acc[item.category] = []
    acc[item.category].push(item)
    return acc
  }, {} as Record<KnowledgeCategory, KnowledgeItem[]>)

  return (
    <div className="h-full flex flex-col">
      <div className="flex-1 min-h-0">
        <Command className="h-full">
          <div className="p-3 border-b flex items-center justify-between">
            <span className="text-sm font-medium">Knowledge</span>
            <Button size="sm" variant="ghost" className="h-7 px-2">
              <Plus className="h-3 w-3 mr-1" />
              Add
            </Button>
          </div>
          <div className="px-2 pt-2">
            <CommandInput placeholder="Search..." />
          </div>
          <CommandList className="h-full">
            <ScrollArea className="h-full">
              <div className="p-2 pb-4 space-y-1">
                {(Object.keys(categoryMeta) as KnowledgeCategory[]).map((category, index) => {
                  const meta = categoryMeta[category]
                  const Icon = meta.icon
                  const categoryItems = itemsByCategory[category] || []

                  return (
                    <div key={category}>
                      {index > 0 && <CommandSeparator />}
                      <CommandGroup heading={meta.label}>
                        {categoryItems.map((item) => (
                          <ContextMenu key={item.id}>
                            <ContextMenuTrigger asChild>
                              <CommandItem
                                value={item.name}
                                onSelect={() => onSelect(item)}
                                className={cn(
                                  selectedId === item.id && 'bg-secondary text-foreground'
                                )}
                              >
                                <Icon className={cn('h-4 w-4', categoryColors[item.category].icon)} />
                                <span className="text-sm truncate">{item.name}</span>
                              </CommandItem>
                            </ContextMenuTrigger>
                            <ContextMenuContent>
                              <ContextMenuItem onSelect={() => onSelect(item)}>
                                Open
                              </ContextMenuItem>
                              <ContextMenuSeparator />
                              <ContextMenuItem onSelect={() => console.log('TODO: reveal file', item.file)}>
                                Reveal file
                              </ContextMenuItem>
                              <ContextMenuItem onSelect={() => console.log('TODO: remove item', item.id)}>
                                Remove
                              </ContextMenuItem>
                            </ContextMenuContent>
                          </ContextMenu>
                        ))}
                        {categoryItems.length === 0 && (
                          <div className="px-2 py-1.5 text-xs text-muted-foreground">No items yet</div>
                        )}
                      </CommandGroup>
                    </div>
                  )
                })}
                <CommandEmpty>
                  <div className="py-8 text-center text-muted-foreground">
                    <File className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p className="text-sm">No matching knowledge entries</p>
                    <p className="text-xs mt-1">Try a different search</p>
                  </div>
                </CommandEmpty>
              </div>
            </ScrollArea>
          </CommandList>
        </Command>
      </div>
    </div>
  )
}

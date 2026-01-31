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
} from '@/components/ui/command'
import type { Brief, BriefStage } from '@/types'
import { Plus, Zap } from 'lucide-react'
import { stageConfig, calculateDecay, formatDaysRemaining } from './utils'

interface BriefListProps {
  briefs: Brief[]
  selectedId?: string
  onSelect: (brief: Brief) => void
  onCreate: () => void
}

export function BriefList({ briefs, selectedId, onSelect, onCreate }: BriefListProps) {
  // Group briefs by stage
  const briefsByStage = briefs.reduce((acc, brief) => {
    if (!acc[brief.stage]) acc[brief.stage] = []
    acc[brief.stage].push(brief)
    return acc
  }, {} as Record<BriefStage, Brief[]>)

  const stages: BriefStage[] = ['spark', 'grounded', 'ready']

  return (
    <div className="h-full flex flex-col">
      <div className="flex-1 min-h-0">
        <Command className="h-full">
          <div className="p-3 border-b flex items-center justify-between">
            <span className="text-sm font-medium">Briefs</span>
            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={onCreate}>
              <Plus className="h-3 w-3 mr-1" />
              New
            </Button>
          </div>
          <div className="px-2 pt-2">
            <CommandInput placeholder="Search briefs..." />
          </div>
          <CommandList className="h-full">
            <ScrollArea className="h-full">
              <div className="p-2 pb-4 space-y-1">
                {stages.map((stage) => {
                  const config = stageConfig[stage]
                  const Icon = config.icon
                  const stageBriefs = briefsByStage[stage] || []

                  return (
                    <CommandGroup key={stage} heading={config.label}>
                      {stageBriefs.map((brief) => {
                        const decay = calculateDecay(brief.createdAt)
                        const daysLeft = formatDaysRemaining(brief.createdAt)

                        return (
                          <CommandItem
                            key={brief.id}
                            value={brief.title}
                            onSelect={() => onSelect(brief)}
                            className={cn(
                              'flex items-center gap-2',
                              selectedId === brief.id && 'bg-secondary text-foreground'
                            )}
                            style={{ opacity: decay }}
                          >
                            <Icon className={cn('h-4 w-4 shrink-0', config.color, config.filled && 'fill-current')} />
                            <div className="flex-1 min-w-0">
                              <div className="text-sm truncate">{brief.title}</div>
                              <div className="text-xs text-muted-foreground truncate">
                                {brief.intent}
                              </div>
                            </div>
                            <span className="text-[10px] text-muted-foreground shrink-0">
                              {daysLeft}
                            </span>
                          </CommandItem>
                        )
                      })}
                      {stageBriefs.length === 0 && (
                        <div className="px-2 py-1.5 text-xs text-muted-foreground">
                          No {config.label.toLowerCase()} briefs
                        </div>
                      )}
                    </CommandGroup>
                  )
                })}
                <CommandEmpty>
                  <div className="py-8 text-center text-muted-foreground">
                    <Zap className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p className="text-sm">No matching briefs</p>
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

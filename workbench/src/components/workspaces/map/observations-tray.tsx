import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Card, CardContent } from '@/components/ui/card'
import type { Observation } from '@/types'
import { Lightbulb, Check, X, Ban, Sparkles } from 'lucide-react'
import { categoryMeta, categoryColors } from './utils'

// Confidence indicator component - thin progress bar
function ConfidenceBar({ confidence }: { confidence: number }) {
  const percent = Math.round(confidence * 100)
  return (
    <div
      role="meter"
      aria-label="Confidence"
      aria-valuenow={percent}
      aria-valuemin={0}
      aria-valuemax={100}
      className="w-8 h-1 rounded-full bg-muted-foreground/20 overflow-hidden"
      title={`${percent}% confidence`}
    >
      <div
        className="h-full bg-muted-foreground/60 rounded-full transition-all"
        style={{ width: `${percent}%` }}
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

interface ObservationsTrayProps {
  observations: Observation[]
}

export function ObservationsTray({ observations }: ObservationsTrayProps) {
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
              <Card
                key={obs.id}
                className={cn(
                  'group transition-colors shadow-none',
                  isTop
                    ? 'border-primary/40 hover:border-primary/60'
                    : 'hover:border-muted-foreground/40'
                )}
              >
                <CardContent className="p-3">
                  {/* Header: Category badge + Confidence */}
                  <div className="flex items-center justify-between mb-2">
                    <Badge
                      variant="outline"
                      className={cn('text-[10px] font-medium px-1.5 py-0.5', categoryColors[obs.category].badge)}
                    >
                      {meta.label}
                    </Badge>
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
                </CardContent>
              </Card>
            )
          })}
        </div>
      </ScrollArea>
    </div>
  )
}

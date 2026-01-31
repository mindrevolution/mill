import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import type { DroppedBrief } from '@/types'
import { Archive, Trash2 } from 'lucide-react'

interface DroppedTrayProps {
  dropped: DroppedBrief[]
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays} days ago`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`
  return date.toLocaleDateString()
}

export function DroppedTray({ dropped }: DroppedTrayProps) {
  if (dropped.length === 0) {
    return (
      <div className="h-full flex items-center justify-center text-muted-foreground">
        <div className="text-center">
          <Archive className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">No dropped briefs</p>
          <p className="text-xs mt-1 opacity-70">Ideas that didn't make it</p>
        </div>
      </div>
    )
  }

  // Sort by most recently dropped
  const sorted = [...dropped].sort(
    (a, b) => new Date(b.droppedAt).getTime() - new Date(a.droppedAt).getTime()
  )

  return (
    <div className="h-full flex flex-col">
      <div className="p-3 border-b flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Trash2 className="h-4 w-4 text-muted-foreground" />
          <span className="text-sm font-medium">Dropped</span>
          <Badge variant="secondary">{dropped.length}</Badge>
        </div>
      </div>
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-2">
          {sorted.map((item) => (
            <Card
              key={item.id}
              className="group transition-colors shadow-none hover:border-muted-foreground/40"
            >
              <CardContent className="p-3">
                {/* Title */}
                <p className="text-sm font-medium text-muted-foreground line-through">
                  {item.originalTitle}
                </p>

                {/* Essence */}
                <p className="text-xs text-muted-foreground mt-1 italic">
                  "{item.essence}"
                </p>

                {/* Date */}
                <p className="text-[10px] text-muted-foreground/60 mt-2">
                  {formatDate(item.droppedAt)}
                </p>
              </CardContent>
            </Card>
          ))}
        </div>
      </ScrollArea>
    </div>
  )
}

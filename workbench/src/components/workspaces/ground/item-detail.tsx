import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { FloatingActionBar } from '@/components/ui/floating-action-bar'
import type { KnowledgeItem } from '@/types'
import { FolderOpen, Terminal, Trash2 } from 'lucide-react'
import { categoryMeta } from './utils'

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'today'
  if (diffDays === 1) return 'yesterday'
  if (diffDays < 7) return `${diffDays} days ago`
  if (diffDays < 14) return '1 week ago'
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`
  if (diffDays < 60) return '1 month ago'
  if (diffDays < 365) return `${Math.floor(diffDays / 30)} months ago`
  return `${Math.floor(diffDays / 365)} year${Math.floor(diffDays / 365) > 1 ? 's' : ''} ago`
}

interface ItemDetailProps {
  item?: KnowledgeItem
}

export function ItemDetail({ item }: ItemDetailProps) {
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
    <div className="h-full relative">
      <ScrollArea className="h-full">
        <div className="p-4 space-y-4">
          {/* Header */}
          <div>
            <div className="flex items-center gap-2 mb-2">
              <Icon className="h-4 w-4 text-muted-foreground" />
              <Badge variant="secondary">{meta.label}</Badge>
            </div>
            <h2 className="text-lg font-semibold">{item.name}</h2>
            <p className="text-sm text-muted-foreground mt-1">{item.description}</p>
          </div>

          <Separator />

          {/* File info */}
          <div className="space-y-1">
            <span className="font-mono text-xs text-muted-foreground">{item.file}</span>
            <p className="text-xs text-muted-foreground/70">
              Updated {formatRelativeTime(item.updatedAt)}
            </p>
          </div>

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
          title="Delete"
          onClick={() => console.log('TODO: Delete')}
        >
          <Trash2 className="h-4 w-4" />
        </Button>
      </FloatingActionBar>
    </div>
  )
}

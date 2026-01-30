import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import type { LibraryItem } from '@/types'
import { FolderOpen } from 'lucide-react'
import { categoryMeta } from './utils'

interface ItemDetailProps {
  item?: LibraryItem
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

import * as React from 'react'
import { cn } from '@/lib/utils'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Tooltip } from '@/components/ui/tooltip'
import {
  Inbox,
  FileText,
  Send,
  Trash2,
  Archive,
  AlertCircle,
  Users,
  Clock,
  MessageSquare,
  ShoppingBag,
  Tag,
  ChevronDown,
  Settings,
} from 'lucide-react'

interface NavItemProps {
  icon: React.ReactNode
  label: string
  count?: number
  active?: boolean
  onClick?: () => void
}

function NavItem({ icon, label, count, active, onClick }: NavItemProps) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors',
        active
          ? 'bg-secondary text-foreground'
          : 'text-muted-foreground hover:text-foreground hover:bg-secondary/50'
      )}
    >
      <span className="shrink-0">{icon}</span>
      <span className="flex-1 text-left truncate">{label}</span>
      {count !== undefined && (
        <span className="text-xs tabular-nums">{count}</span>
      )}
    </button>
  )
}

interface SidebarProps {
  className?: string
}

export function Sidebar({ className }: SidebarProps) {
  const [activeItem, setActiveItem] = React.useState('inbox')

  return (
    <div className={cn('flex flex-col h-full bg-background', className)}>
      {/* Header */}
      <div className="p-4 flex items-center gap-2">
        <div className="h-4 w-4 rounded-sm bg-primary" />
        <span className="font-semibold">mill</span>
        <ChevronDown className="h-4 w-4 text-muted-foreground ml-auto" />
      </div>

      <Separator />

      {/* Navigation */}
      <ScrollArea className="flex-1 px-2 py-2">
        <nav className="space-y-1">
          <NavItem
            icon={<Inbox className="h-4 w-4" />}
            label="Inbox"
            count={128}
            active={activeItem === 'inbox'}
            onClick={() => setActiveItem('inbox')}
          />
          <NavItem
            icon={<FileText className="h-4 w-4" />}
            label="Drafts"
            count={9}
            active={activeItem === 'drafts'}
            onClick={() => setActiveItem('drafts')}
          />
          <NavItem
            icon={<Send className="h-4 w-4" />}
            label="Sent"
            active={activeItem === 'sent'}
            onClick={() => setActiveItem('sent')}
          />
          <NavItem
            icon={<AlertCircle className="h-4 w-4" />}
            label="Junk"
            count={23}
            active={activeItem === 'junk'}
            onClick={() => setActiveItem('junk')}
          />
          <NavItem
            icon={<Trash2 className="h-4 w-4" />}
            label="Trash"
            active={activeItem === 'trash'}
            onClick={() => setActiveItem('trash')}
          />
          <NavItem
            icon={<Archive className="h-4 w-4" />}
            label="Archive"
            active={activeItem === 'archive'}
            onClick={() => setActiveItem('archive')}
          />
        </nav>

        <Separator className="my-4" />

        <nav className="space-y-1">
          <NavItem
            icon={<Users className="h-4 w-4" />}
            label="Social"
            count={972}
            active={activeItem === 'social'}
            onClick={() => setActiveItem('social')}
          />
          <NavItem
            icon={<Clock className="h-4 w-4" />}
            label="Updates"
            count={342}
            active={activeItem === 'updates'}
            onClick={() => setActiveItem('updates')}
          />
          <NavItem
            icon={<MessageSquare className="h-4 w-4" />}
            label="Forums"
            count={128}
            active={activeItem === 'forums'}
            onClick={() => setActiveItem('forums')}
          />
          <NavItem
            icon={<ShoppingBag className="h-4 w-4" />}
            label="Shopping"
            count={8}
            active={activeItem === 'shopping'}
            onClick={() => setActiveItem('shopping')}
          />
          <NavItem
            icon={<Tag className="h-4 w-4" />}
            label="Promotions"
            count={21}
            active={activeItem === 'promotions'}
            onClick={() => setActiveItem('promotions')}
          />
        </nav>
      </ScrollArea>

      <Separator />

      {/* Footer */}
      <div className="p-2">
        <Tooltip content="Settings" side="right">
          <button className="w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm text-muted-foreground hover:text-foreground hover:bg-secondary/50 transition-colors">
            <Settings className="h-4 w-4" />
            <span>Settings</span>
          </button>
        </Tooltip>
      </div>
    </div>
  )
}

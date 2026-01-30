import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Separator } from '@/components/ui/separator'
import { Tooltip } from '@/components/ui/tooltip'
import {
  Archive,
  ArchiveX,
  Trash2,
  Clock,
  Reply,
  ReplyAll,
  Forward,
  MoreVertical,
  Send,
} from 'lucide-react'

interface MessageDetailProps {
  className?: string
}

export function MessageDetail({ className }: MessageDetailProps) {
  return (
    <div className={cn('flex flex-col h-full', className)}>
      {/* Toolbar */}
      <div className="flex items-center justify-between p-2 border-b">
        <div className="flex items-center gap-1">
          <Tooltip content="Archive">
            <Button variant="ghost" size="icon">
              <Archive className="h-4 w-4" />
            </Button>
          </Tooltip>
          <Tooltip content="Move to junk">
            <Button variant="ghost" size="icon">
              <ArchiveX className="h-4 w-4" />
            </Button>
          </Tooltip>
          <Tooltip content="Delete">
            <Button variant="ghost" size="icon">
              <Trash2 className="h-4 w-4 text-destructive" />
            </Button>
          </Tooltip>
          <Separator orientation="vertical" className="mx-1 h-6" />
          <Tooltip content="Snooze">
            <Button variant="ghost" size="icon">
              <Clock className="h-4 w-4" />
            </Button>
          </Tooltip>
        </div>
        <div className="flex items-center gap-1">
          <Tooltip content="Reply">
            <Button variant="ghost" size="icon">
              <Reply className="h-4 w-4" />
            </Button>
          </Tooltip>
          <Tooltip content="Reply all">
            <Button variant="ghost" size="icon">
              <ReplyAll className="h-4 w-4" />
            </Button>
          </Tooltip>
          <Tooltip content="Forward">
            <Button variant="ghost" size="icon">
              <Forward className="h-4 w-4" />
            </Button>
          </Tooltip>
          <Separator orientation="vertical" className="mx-1 h-6" />
          <Tooltip content="More">
            <Button variant="ghost" size="icon">
              <MoreVertical className="h-4 w-4" />
            </Button>
          </Tooltip>
        </div>
      </div>

      {/* Message Header */}
      <div className="p-6 border-b">
        <div className="flex items-start gap-4">
          <Avatar>
            <AvatarFallback className="bg-primary/20 text-primary">WS</AvatarFallback>
          </Avatar>
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="font-semibold">William Smith</h2>
                <p className="text-sm text-muted-foreground">Meeting Tomorrow</p>
              </div>
              <span className="text-sm text-muted-foreground whitespace-nowrap">
                Oct 22, 2023, 9:00:00 AM
              </span>
            </div>
            <p className="text-sm text-muted-foreground mt-1">
              Reply-To: williamsmith@example.com
            </p>
          </div>
        </div>
      </div>

      {/* Message Body */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="prose prose-invert prose-sm max-w-none">
          <p>
            Hi, let's have a meeting tomorrow to discuss the project. I've been
            reviewing the project details and have some ideas I'd like to share.
            It's crucial that we align on our next steps to ensure the project's
            success.
          </p>
          <p>
            Please come prepared with any questions or insights you may have.
            Looking forward to our meeting!
          </p>
          <p>Best regards, William</p>
        </div>
      </div>

      {/* Reply */}
      <div className="p-4 border-t">
        <div className="flex items-center gap-3">
          <Input
            placeholder="Reply William Smith..."
            className="flex-1"
          />
          <Button>
            <Send className="h-4 w-4 mr-2" />
            Send
          </Button>
        </div>
        <div className="flex items-center gap-4 mt-3">
          <label className="flex items-center gap-2 text-sm text-muted-foreground">
            <input type="checkbox" className="rounded border-border" />
            Mute this thread
          </label>
        </div>
      </div>
    </div>
  )
}

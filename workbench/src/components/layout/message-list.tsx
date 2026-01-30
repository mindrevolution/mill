import { cn } from '@/lib/utils'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Search } from 'lucide-react'

interface Message {
  id: string
  from: string
  subject: string
  preview: string
  date: string
  tags: { label: string; variant?: 'default' | 'secondary' }[]
  unread?: boolean
}

const messages: Message[] = [
  {
    id: '1',
    from: 'William Smith',
    subject: 'Meeting Tomorrow',
    preview: "Hi, let's have a meeting tomorrow to discuss the project. I've been reviewing the project details and have some ideas I'd like to share. It's...",
    date: 'over 2 years ago',
    tags: [
      { label: 'meeting', variant: 'secondary' },
      { label: 'work', variant: 'default' },
      { label: 'important', variant: 'secondary' },
    ],
  },
  {
    id: '2',
    from: 'Alice Smith',
    subject: 'Re: Project Update',
    preview: "Thank you for the project update. It looks great! I've gone through the report, and the progress is impressive. The team has done a fantastic jo...",
    date: 'over 2 years ago',
    tags: [
      { label: 'work', variant: 'default' },
      { label: 'important', variant: 'secondary' },
    ],
  },
  {
    id: '3',
    from: 'Bob Johnson',
    subject: 'Weekend Plans',
    preview: "Any plans for the weekend? I was thinking of going hiking in the nearby mountains. It's been a while since we had some outdoor fun. If you're...",
    date: 'almost 3 years ago',
    tags: [{ label: 'personal', variant: 'secondary' }],
  },
  {
    id: '4',
    from: 'Emily Davis',
    subject: 'Re: Question about Budget',
    preview: "I have a question about the budget for the upcoming project. It seems like there's a discrepancy in the allocation of resources. I've reviewed the...",
    date: 'almost 3 years ago',
    tags: [
      { label: 'work', variant: 'default' },
      { label: 'budget', variant: 'secondary' },
    ],
    unread: true,
  },
  {
    id: '5',
    from: 'Michael Wilson',
    subject: 'Important Announcement',
    preview: "I have an important announcement to make during our team meeting. It pertains to a strategic shift in our approach to the upcoming product...",
    date: 'almost 3 years ago',
    tags: [],
    unread: true,
  },
]

interface MessageListProps {
  className?: string
  selectedId?: string
  onSelect?: (id: string) => void
}

export function MessageList({ className, selectedId, onSelect }: MessageListProps) {
  return (
    <div className={cn('flex flex-col h-full', className)}>
      {/* Header */}
      <div className="p-4 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-xl font-semibold">Inbox</h2>
          <div className="flex items-center gap-2 text-sm">
            <button className="text-foreground">All mail</button>
            <button className="text-muted-foreground hover:text-foreground">Unread</button>
          </div>
        </div>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input placeholder="Search" className="pl-9" />
        </div>
      </div>

      {/* List */}
      <ScrollArea className="flex-1">
        <div className="px-2 pb-2">
          {messages.map((message) => (
            <button
              key={message.id}
              onClick={() => onSelect?.(message.id)}
              className={cn(
                'w-full text-left p-4 rounded-lg border mb-2 transition-colors',
                selectedId === message.id
                  ? 'bg-card border-border'
                  : 'border-transparent hover:bg-card/50'
              )}
            >
              <div className="flex items-start justify-between gap-2 mb-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium">{message.from}</span>
                  {message.unread && (
                    <span className="h-2 w-2 rounded-full bg-blue-500" />
                  )}
                </div>
                <span className="text-xs text-muted-foreground whitespace-nowrap">
                  {message.date}
                </span>
              </div>
              <div className="text-sm font-medium mb-1">{message.subject}</div>
              <div className="text-sm text-muted-foreground line-clamp-2 mb-2">
                {message.preview}
              </div>
              {message.tags.length > 0 && (
                <div className="flex gap-1.5 flex-wrap">
                  {message.tags.map((tag) => (
                    <Badge key={tag.label} variant={tag.variant}>
                      {tag.label}
                    </Badge>
                  ))}
                </div>
              )}
            </button>
          ))}
        </div>
      </ScrollArea>
    </div>
  )
}

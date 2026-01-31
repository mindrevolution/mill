import { ScrollArea } from '@/components/ui/scroll-area'

interface DetailViewProps {
  header: React.ReactNode
  children: React.ReactNode
  actions?: React.ReactNode
}

export function DetailView({ header, children, actions }: DetailViewProps) {
  return (
    <div className="h-full flex flex-col relative">
      {/* Fixed Header */}
      <div className="p-4 pb-3 border-b shrink-0">
        {header}
      </div>

      {/* Scrollable Body */}
      <ScrollArea className="flex-1 min-h-0">
        <div className="p-4 pb-16">
          {children}
        </div>
      </ScrollArea>

      {/* Floating Actions (optional) */}
      {actions}
    </div>
  )
}

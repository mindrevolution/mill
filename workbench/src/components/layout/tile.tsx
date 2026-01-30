import { cn } from '@/lib/utils'
import { X } from 'lucide-react'

interface TileProps {
  children: React.ReactNode
  title?: string
  className?: string
  focused?: boolean
  onFocus?: () => void
  onClose?: () => void
  toolbar?: React.ReactNode
}

export function Tile({
  children,
  title,
  className,
  focused,
  onFocus,
  onClose,
  toolbar,
}: TileProps) {
  return (
    <div
      onClick={onFocus}
      className={cn(
        'flex flex-col h-full bg-background border rounded-lg overflow-hidden transition-colors',
        focused ? 'border-primary/50' : 'border-border',
        className
      )}
    >
      {(title || toolbar || onClose) && (
        <div className="flex items-center justify-between px-3 py-2 border-b bg-card/50">
          {title && (
            <span className="text-sm font-medium text-muted-foreground">{title}</span>
          )}
          <div className="flex items-center gap-1 ml-auto">
            {toolbar}
            {onClose && (
              <button
                onClick={(e) => {
                  e.stopPropagation()
                  onClose()
                }}
                className="p-1 rounded hover:bg-secondary text-muted-foreground hover:text-foreground transition-colors"
              >
                <X className="h-3 w-3" />
              </button>
            )}
          </div>
        </div>
      )}
      <div className="flex-1 overflow-auto">{children}</div>
    </div>
  )
}

interface TileSplitProps {
  direction: 'horizontal' | 'vertical'
  children: React.ReactNode
  sizes?: number[] // percentages
  className?: string
}

export function TileSplit({ direction, children, sizes, className }: TileSplitProps) {
  const childArray = Array.isArray(children) ? children : [children]
  const defaultSize = 100 / childArray.length

  return (
    <div
      className={cn(
        'flex h-full gap-1',
        direction === 'horizontal' ? 'flex-row' : 'flex-col',
        className
      )}
    >
      {childArray.map((child, i) => (
        <div
          key={i}
          className="min-w-0 min-h-0"
          style={{
            flex: `0 0 ${sizes?.[i] ?? defaultSize}%`,
          }}
        >
          {child}
        </div>
      ))}
    </div>
  )
}

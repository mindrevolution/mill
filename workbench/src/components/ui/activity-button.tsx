import * as React from 'react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'

interface ActivityButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  active?: boolean
  pulsing?: boolean
  count?: number
  children: React.ReactNode
}

export const ActivityButton = React.forwardRef<HTMLButtonElement, ActivityButtonProps>(
  ({ active = false, pulsing = false, count, children, className, ...props }, ref) => {
    return (
      <Button
        ref={ref}
        variant="ghost"
        size="sm"
        className={cn(
          'h-8 px-2 gap-1.5',
          active && 'bg-primary hover:bg-primary/15 text-primary-foreground hover:text-foreground',
          pulsing && 'animate-pulse',
          className
        )}
        {...props}
      >
        {children}
        {count !== undefined && (
          <span className="text-xs tabular-nums min-w-[1ch]">{count}</span>
        )}
      </Button>
    )
  }
)

ActivityButton.displayName = 'ActivityButton'

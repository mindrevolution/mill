import * as React from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { FloatingActionBar } from '@/components/ui/floating-action-bar'

interface ActionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: React.ReactNode
  description?: React.ReactNode
  actions: React.ReactNode
  children: React.ReactNode
  className?: string
}

export function ActionDialog({
  open,
  onOpenChange,
  title,
  description,
  actions,
  children,
  className,
}: ActionDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={`max-h-[85vh] overflow-hidden flex flex-col ${className ?? ''}`}
        hideCloseButton
      >
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>

        <div className="flex-1 min-h-0 overflow-y-auto -mx-6 px-6 pb-16">
          {children}
        </div>

        <FloatingActionBar>
          {actions}
        </FloatingActionBar>
      </DialogContent>
    </Dialog>
  )
}

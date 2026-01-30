import * as React from 'react'
import { cn } from '@/lib/utils'
import { inputVariants } from './variants'

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    const isFile = type === 'file'
    return (
      <input
        type={type}
        className={cn(inputVariants({ variant: isFile ? 'file' : 'default' }), className)}
        ref={ref}
        {...props}
      />
    )
  }
)
Input.displayName = 'Input'

export { Input }

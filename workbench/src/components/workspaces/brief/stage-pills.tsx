import { cn } from '@/lib/utils'
import type { BriefStage } from '@/types'
import { ChevronRight } from 'lucide-react'
import { stageConfig } from './utils'

interface StagePillsProps {
  value: BriefStage
  onChange: (stage: BriefStage) => void
}

const stages: BriefStage[] = ['spark', 'grounded', 'ready']

export function StagePills({ value, onChange }: StagePillsProps) {
  return (
    <div className="flex items-center gap-1">
      {stages.map((stage, index) => {
        const config = stageConfig[stage]
        const Icon = config.icon
        const isActive = value === stage
        const isPast = stages.indexOf(value) > index

        return (
          <div key={stage} className="flex items-center">
            <button
              onClick={() => onChange(stage)}
              className={cn(
                'flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors',
                isActive
                  ? config.badge
                  : isPast
                    ? 'bg-muted/50 text-muted-foreground hover:bg-muted'
                    : 'bg-transparent text-muted-foreground/60 hover:bg-muted/50 hover:text-muted-foreground'
              )}
            >
              <Icon className={cn(
                'h-3.5 w-3.5',
                isActive && config.color,
                config.filled && 'fill-current'
              )} />
              <span>{config.label}</span>
            </button>
            {index < stages.length - 1 && (
              <ChevronRight className="h-4 w-4 text-muted-foreground/40 mx-0.5" />
            )}
          </div>
        )
      })}
    </div>
  )
}

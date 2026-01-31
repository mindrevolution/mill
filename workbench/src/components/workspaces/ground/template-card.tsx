import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { TemplateSummary } from '@/lib/api'

interface TemplateCardProps {
  template: TemplateSummary
  selected: boolean
  onSelect: (id: string) => void
}

export function TemplateCard({ template, selected, onSelect }: TemplateCardProps) {
  return (
    <button
      type="button"
      onClick={() => onSelect(template.id)}
      className={cn(
        'relative flex flex-col gap-1.5 rounded-lg border p-3 text-left transition-all',
        'hover:border-border hover:bg-muted/50',
        selected
          ? 'border-[#ffcc00] bg-[#ffcc00]/10'
          : 'border-border/50 bg-background'
      )}
    >
      {selected && (
        <div className="absolute right-2 top-2 rounded-full bg-[#ffcc00] p-0.5">
          <Check className="h-3 w-3 text-black" />
        </div>
      )}
      <span className="font-medium text-sm">{template.label}</span>
      <span className="text-xs text-muted-foreground line-clamp-2">{template.summary}</span>
      {template.tags.length > 0 && (
        <div className="flex flex-wrap gap-1 mt-1">
          {template.tags.map((tag) => (
            <span
              key={tag}
              className="inline-flex items-center rounded-full bg-muted px-2 py-0.5 text-[10px] text-muted-foreground"
            >
              {tag}
            </span>
          ))}
        </div>
      )}
    </button>
  )
}

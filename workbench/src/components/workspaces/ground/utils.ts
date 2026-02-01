import { Users, BookOpen, Lightbulb, Palette } from 'lucide-react'
import type { KnowledgeCategory } from '@/types'

export const categoryMeta: Record<KnowledgeCategory, { label: string; icon: typeof Users; description: string }> = {
  personas: { label: 'Personas', icon: Users, description: 'Who you build for' },
  standards: { label: 'Standards', icon: BookOpen, description: 'How you build' },
  concepts: { label: 'Concepts', icon: Lightbulb, description: 'Domain vocabulary' },
  design: { label: 'Design', icon: Palette, description: 'Visual language' },
}

export const categoryColors: Record<KnowledgeCategory, { badge: string; icon: string }> = {
  personas: { badge: 'bg-blue-500/15 text-blue-400', icon: 'text-blue-400' },
  standards: { badge: 'bg-emerald-500/15 text-emerald-400', icon: 'text-emerald-400' },
  concepts: { badge: 'bg-violet-500/15 text-violet-400', icon: 'text-violet-400' },
  design: { badge: 'bg-pink-500/15 text-pink-400', icon: 'text-pink-400' },
}

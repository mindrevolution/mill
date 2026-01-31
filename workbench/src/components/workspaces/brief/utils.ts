import { Circle, CircleDot } from 'lucide-react'
import type { BriefStage } from '@/types'

export const stageConfig: Record<BriefStage, {
  label: string
  icon: typeof Circle
  color: string
  badge: string
  description: string
  filled?: boolean
}> = {
  spark: {
    label: 'Spark',
    icon: Circle,
    color: 'text-amber-400',
    badge: 'bg-amber-500/15 text-amber-400',
    description: 'Raw idea, needs validation',
  },
  grounded: {
    label: 'Grounded',
    icon: CircleDot,
    color: 'text-blue-400',
    badge: 'bg-blue-500/15 text-blue-400',
    description: 'Validated, being refined',
  },
  ready: {
    label: 'Ready',
    icon: Circle,
    color: 'text-emerald-400',
    badge: 'bg-emerald-500/15 text-emerald-400',
    description: 'Ready to promote to Shape',
    filled: true,
  },
}

/**
 * Calculate opacity decay based on age (30-day time-box)
 * Day 0: opacity 1.0 (fully solid)
 * Day 15: opacity 0.65
 * Day 30+: opacity 0.3 (faded but visible)
 */
export function calculateDecay(createdAt: string): number {
  const created = new Date(createdAt)
  const now = new Date()
  const daysOld = Math.floor((now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24))
  return Math.max(0.3, 1 - (daysOld / 30) * 0.7)
}

/**
 * Get days remaining before brief expires (30-day window)
 */
export function daysRemaining(createdAt: string): number {
  const created = new Date(createdAt)
  const now = new Date()
  const daysOld = Math.floor((now.getTime() - created.getTime()) / (1000 * 60 * 60 * 24))
  return Math.max(0, 30 - daysOld)
}

/**
 * Format days remaining as human-readable text
 */
export function formatDaysRemaining(createdAt: string): string {
  const days = daysRemaining(createdAt)
  if (days === 0) return 'Expired'
  if (days === 1) return '1 day left'
  return `${days} days left`
}

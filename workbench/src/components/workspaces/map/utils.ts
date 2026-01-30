import { Users, BookOpen, Lightbulb, Palette } from 'lucide-react'
import type { LibraryCategory, LibraryItem, Observation } from '@/types'

export const categoryMeta: Record<LibraryCategory, { label: string; icon: typeof Users; description: string }> = {
  personas: { label: 'Personas', icon: Users, description: 'Who you build for' },
  standards: { label: 'Standards', icon: BookOpen, description: 'How you build' },
  concepts: { label: 'Concepts', icon: Lightbulb, description: 'Domain vocabulary' },
  design: { label: 'Design', icon: Palette, description: 'Visual language' },
}

export const categoryColors: Record<LibraryCategory, { badge: string; icon: string }> = {
  personas: { badge: 'bg-blue-500/15 text-blue-400', icon: 'text-blue-400' },
  standards: { badge: 'bg-emerald-500/15 text-emerald-400', icon: 'text-emerald-400' },
  concepts: { badge: 'bg-violet-500/15 text-violet-400', icon: 'text-violet-400' },
  design: { badge: 'bg-pink-500/15 text-pink-400', icon: 'text-pink-400' },
}

// Mock data - will be replaced with API calls
export const mockLibrary: LibraryItem[] = [
  { id: '1', category: 'personas', name: 'Mobile User', description: 'Users primarily on mobile devices, often with spotty connectivity', file: 'mobile-user.md', createdAt: '2024-01-15', updatedAt: '2024-01-20' },
  { id: '2', category: 'personas', name: 'Power User', description: 'Technical users who want keyboard shortcuts and advanced features', file: 'power-user.md', createdAt: '2024-01-10', updatedAt: '2024-01-10' },
  { id: '3', category: 'standards', name: 'Async I/O', description: 'Use async/await for all I/O operations', file: 'async-io.md', createdAt: '2024-01-12', updatedAt: '2024-01-12' },
  { id: '4', category: 'standards', name: 'Error Handling', description: 'Return Result<T> instead of throwing exceptions', file: 'error-handling.md', createdAt: '2024-01-08', updatedAt: '2024-01-18' },
  { id: '5', category: 'concepts', name: 'Match', description: 'A game session between two players', file: 'match.md', createdAt: '2024-01-05', updatedAt: '2024-01-05' },
  { id: '6', category: 'concepts', name: 'Player', description: 'A user participating in matches', file: 'player.md', createdAt: '2024-01-05', updatedAt: '2024-01-05' },
  { id: '7', category: 'design', name: 'Color Tokens', description: 'Primary, secondary, and accent colors', file: 'colors.md', createdAt: '2024-01-02', updatedAt: '2024-01-15' },
]

export const mockObservations: Observation[] = [
  { id: '1', category: 'personas', suggestion: 'Enterprise Admin — mentioned in 4 recent specs', source: 'spec:user-roles, spec:permissions', confidence: 0.85, createdAt: '2 hours ago' },
  { id: '2', category: 'standards', suggestion: 'Validation at boundaries — pattern in auth, API modules', source: 'run:41, run:38', confidence: 0.72, createdAt: '1 day ago' },
  { id: '3', category: 'concepts', suggestion: 'Tournament — appears in match-related specs', source: 'spec:tournaments, spec:rankings', confidence: 0.68, createdAt: '2 days ago' },
]

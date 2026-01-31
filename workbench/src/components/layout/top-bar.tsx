import { cn } from '@/lib/utils'
import type { Workspace } from '@/types'
import type { Job } from '@/lib/api'
import { LandPlot, SquareStack, Orbit, Rocket, Settings, Lightbulb } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { JobsIndicator } from '@/components/jobs'

interface TopBarProps {
  active: Workspace
  onSwitch: (workspace: Workspace) => void
  onSettings?: () => void
  onViewJobResult?: (job: Job) => void
  activeRuns?: number
  openObservations?: number
}

const workspaces: { id: Workspace; label: string; icon: typeof LandPlot; key: string }[] = [
  { id: 'ground', label: 'Ground', icon: LandPlot, key: '1' },
  { id: 'brief', label: 'Brief', icon: SquareStack, key: '2' },
  { id: 'shape', label: 'Shape', icon: Orbit, key: '3' },
  { id: 'ship', label: 'Ship', icon: Rocket, key: '4' },
]

export function TopBar({
  active,
  onSwitch,
  onSettings,
  onViewJobResult,
  activeRuns = 0,
  openObservations = 0,
}: TopBarProps) {
  return (
    <div className="flex items-center h-12 bg-background border-b px-3 gap-4">
      {/* Workspace tabs */}
      <nav className="flex items-center gap-1">
        {workspaces.map(({ id, label, icon: Icon, key }) => (
          <Button
            key={id}
            onClick={() => onSwitch(id)}
            title={`${label} (${key})`}
            variant={active === id ? 'secondary' : 'ghost'}
            size="sm"
            className={cn(
              'h-8 px-3',
              active === id ? 'text-foreground' : 'text-muted-foreground hover:text-foreground'
            )}
          >
            <Icon className="h-4 w-4" />
            <span className="text-sm">{label}</span>
          </Button>
        ))}
      </nav>

      {/* Spacer */}
      <div className="flex-1" />

      {/* Right: Status & actions */}
      <div className="flex items-center gap-2">
        {/* Active runs indicator */}
        {activeRuns > 0 && (
          <div className="flex items-center gap-1.5 px-2 py-1 rounded-md bg-secondary text-sm">
            <div className="w-2 h-2 rounded-full bg-blue-400 animate-pulse" />
            <span className="text-muted-foreground">{activeRuns} running</span>
          </div>
        )}

        {/* Observations indicator */}
        {openObservations > 0 && (
          <Button
            onClick={() => onSwitch('ground')}
            variant="ghost"
            size="sm"
            className="h-8 px-2 bg-primary/10 hover:bg-primary/20 text-primary"
            title="Open observations - click to view in Ground"
          >
            <Lightbulb className="h-3.5 w-3.5 text-primary" />
            <span>{openObservations}</span>
          </Button>
        )}

        {/* Jobs indicator */}
        <JobsIndicator onViewResult={onViewJobResult} />

        {/* Settings */}
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={onSettings}>
          <Settings className="h-4 w-4 text-muted-foreground" />
        </Button>
      </div>
    </div>
  )
}

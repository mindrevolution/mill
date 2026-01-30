import { cn } from '@/lib/utils'
import type { Workspace } from '@/types'
import { PenTool, Map, Rocket, Settings, Lightbulb } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface TopBarProps {
  active: Workspace
  onSwitch: (workspace: Workspace) => void
  projectName: string
  onProjectSwitch?: () => void
  onSettings?: () => void
  activeRuns?: number
  openObservations?: number
}

const workspaces: { id: Workspace; label: string; icon: typeof PenTool; key: string }[] = [
  { id: 'shape', label: 'Shape', icon: PenTool, key: '1' },
  { id: 'map', label: 'Map', icon: Map, key: '2' },
  { id: 'ship', label: 'Ship', icon: Rocket, key: '3' },
]

export function TopBar({
  active,
  onSwitch,
  projectName,
  onProjectSwitch,
  onSettings,
  activeRuns = 0,
  openObservations = 0,
}: TopBarProps) {
  return (
    <div className="flex items-center h-12 bg-background border-b px-3 gap-4">
      {/* Left: Project indicator */}
      <button
        onClick={onProjectSwitch}
        className="w-8 h-8 rounded-md bg-primary flex items-center justify-center hover:opacity-90 transition-opacity"
        title={projectName}
      >
        <span className="text-primary-foreground font-semibold text-sm">
          {projectName.charAt(0).toLowerCase()}
        </span>
      </button>

      {/* Workspace tabs */}
      <nav className="flex items-center gap-1">
        {workspaces.map(({ id, label, icon: Icon, key }) => (
          <button
            key={id}
            onClick={() => onSwitch(id)}
            title={`${label} (${key})`}
            className={cn(
              'flex items-center gap-2 px-3 py-1.5 rounded-md transition-colors',
              active === id
                ? 'bg-secondary text-foreground'
                : 'text-muted-foreground hover:text-foreground hover:bg-secondary/50'
            )}
          >
            <Icon className="h-4 w-4" />
            <span className="text-sm">{label}</span>
          </button>
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
          <button
            onClick={() => onSwitch('map')}
            className="flex items-center gap-1.5 px-2 py-1 rounded-md bg-primary/10 hover:bg-primary/20 transition-colors text-sm"
            title="Open observations - click to view in Map"
          >
            <Lightbulb className="h-3.5 w-3.5 text-primary" />
            <span className="text-primary">{openObservations}</span>
          </button>
        )}

        {/* Settings */}
        <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={onSettings}>
          <Settings className="h-4 w-4 text-muted-foreground" />
        </Button>
      </div>
    </div>
  )
}

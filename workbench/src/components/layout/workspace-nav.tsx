import { cn } from '@/lib/utils'
import { Tooltip } from '@/components/ui/tooltip'
import type { Workspace } from '@/types'
import { PenTool, Map, Rocket, Settings } from 'lucide-react'

interface WorkspaceNavProps {
  active: Workspace
  onSwitch: (workspace: Workspace) => void
  projectName: string
  onProjectSwitch?: () => void
  onSettings?: () => void
}

const workspaces: { id: Workspace; label: string; icon: typeof PenTool; key: string }[] = [
  { id: 'shape', label: 'Shape', icon: PenTool, key: '1' },
  { id: 'map', label: 'Map', icon: Map, key: '2' },
  { id: 'ship', label: 'Ship', icon: Rocket, key: '3' },
]

export function WorkspaceNav({
  active,
  onSwitch,
  projectName,
  onProjectSwitch,
  onSettings,
}: WorkspaceNavProps) {
  return (
    <div className="flex flex-col h-full w-14 bg-background border-r items-center py-3 gap-1">
      {/* Project indicator */}
      <Tooltip content={`${projectName} (⌘O)`} side="right">
        <button
          onClick={onProjectSwitch}
          className="w-9 h-9 rounded-md bg-primary flex items-center justify-center mb-4 hover:opacity-90 transition-opacity"
        >
          <span className="text-primary-foreground font-semibold text-sm">
            {projectName.charAt(0).toLowerCase()}
          </span>
        </button>
      </Tooltip>

      {/* Workspace tabs */}
      <nav className="flex flex-col gap-1 flex-1">
        {workspaces.map(({ id, label, icon: Icon, key }) => (
          <Tooltip key={id} content={`${label} (${key})`} side="right">
            <button
              onClick={() => onSwitch(id)}
              className={cn(
                'w-9 h-9 rounded-md flex items-center justify-center transition-colors',
                active === id
                  ? 'bg-secondary text-foreground'
                  : 'text-muted-foreground hover:text-foreground hover:bg-secondary/50'
              )}
            >
              <Icon className="h-4 w-4" />
            </button>
          </Tooltip>
        ))}
      </nav>

      {/* Bottom actions */}
      <div className="flex flex-col gap-1">
        <Tooltip content="Settings" side="right">
          <button
            onClick={onSettings}
            className="w-9 h-9 rounded-md flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-secondary/50 transition-colors"
          >
            <Settings className="h-4 w-4" />
          </button>
        </Tooltip>
      </div>
    </div>
  )
}

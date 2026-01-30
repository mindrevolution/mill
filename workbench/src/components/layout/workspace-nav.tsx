import { cn } from '@/lib/utils'
import type { Workspace } from '@/types'
import { PenTool, Map, Rocket, Settings } from 'lucide-react'
import { Button } from '@/components/ui/button'

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
    <div className="flex flex-col h-full w-36 bg-background border-r py-3 px-2 gap-1">
      {/* Project indicator */}
      <Button
        variant="ghost"
        className="justify-start gap-2 px-2 mb-3"
        onClick={onProjectSwitch}
      >
        <div className="w-4 h-4 flex items-center justify-center flex-shrink-0">
          <div className="w-2 h-2 rounded-sm bg-primary" />
        </div>
        <span className="text-sm font-medium truncate">{projectName}</span>
      </Button>

      {/* Workspace tabs */}
      <nav className="flex flex-col gap-0.5 flex-1">
        {workspaces.map(({ id, label, icon: Icon, key }) => (
          <Button
            key={id}
            onClick={() => onSwitch(id)}
            variant={active === id ? 'secondary' : 'ghost'}
            size="sm"
            className={cn(
              'w-full justify-start gap-2 px-2',
              active === id
                ? 'text-foreground'
                : 'text-muted-foreground hover:text-foreground'
            )}
          >
            <Icon className="h-4 w-4 flex-shrink-0" />
            <span className="text-sm">{label}</span>
            <span className="text-xs text-muted-foreground ml-auto">{key}</span>
          </Button>
        ))}
      </nav>

      {/* Bottom actions */}
      <div className="flex flex-col gap-0.5">
        <Button
          onClick={onSettings}
          variant="ghost"
          size="sm"
          className="w-full justify-start gap-2 px-2 text-muted-foreground hover:text-foreground"
        >
          <Settings className="h-4 w-4 flex-shrink-0" />
          <span className="text-sm">Settings</span>
        </Button>
      </div>
    </div>
  )
}

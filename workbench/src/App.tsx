import './index.css'
import { useState, useCallback } from 'react'
import { WorkspaceNav } from '@/components/layout/workspace-nav'
import { ShapeWorkspace } from '@/components/workspaces/shape'
import { MapWorkspace } from '@/components/workspaces/map'
import { ShipWorkspace } from '@/components/workspaces/ship'
import { useKeyboard, WORKSPACE_KEYS } from '@/hooks/useKeyboard'
import type { Workspace } from '@/types'

function App() {
  const [activeWorkspace, setActiveWorkspace] = useState<Workspace>('shape')
  const [projectName] = useState('mill')

  // Keyboard shortcuts
  useKeyboard({
    [WORKSPACE_KEYS.shape]: () => setActiveWorkspace('shape'),
    [WORKSPACE_KEYS.map]: () => setActiveWorkspace('map'),
    [WORKSPACE_KEYS.ship]: () => setActiveWorkspace('ship'),
    'mod+o': () => console.log('TODO: Project switcher'),
  }, [])

  const handleProjectSwitch = useCallback(() => {
    console.log('TODO: Open project switcher modal')
  }, [])

  const handleSettings = useCallback(() => {
    console.log('TODO: Open settings modal')
  }, [])

  return (
    <div className="h-screen flex bg-background text-foreground overflow-hidden">
      {/* Workspace navigation */}
      <WorkspaceNav
        active={activeWorkspace}
        onSwitch={setActiveWorkspace}
        projectName={projectName}
        onProjectSwitch={handleProjectSwitch}
        onSettings={handleSettings}
      />

      {/* Workspace content */}
      <main className="flex-1 min-w-0">
        {activeWorkspace === 'shape' && <ShapeWorkspace />}
        {activeWorkspace === 'map' && <MapWorkspace />}
        {activeWorkspace === 'ship' && <ShipWorkspace />}
      </main>
    </div>
  )
}

export default App

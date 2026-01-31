import './index.css'
import { useState, useCallback, useEffect } from 'react'
import { TopBar } from '@/components/layout/top-bar'
import { GroundWorkspace } from '@/components/workspaces/ground'
import { BriefWorkspace } from '@/components/workspaces/brief'
import { ShapeWorkspace } from '@/components/workspaces/shape'
import { ShipWorkspace } from '@/components/workspaces/ship'
import { useKeyboard, WORKSPACE_KEYS } from '@/hooks/useKeyboard'
import { useJobsStore } from '@/stores/jobs'
import type { Workspace } from '@/types'
import type { Job } from '@/lib/api'

function App() {
  const [activeWorkspace, setActiveWorkspace] = useState<Workspace>('ground')
  const setActiveWorkspaceInStore = useJobsStore((s) => s.setActiveWorkspace)

  // Sync active workspace to jobs store
  useEffect(() => {
    setActiveWorkspaceInStore(activeWorkspace)
  }, [activeWorkspace, setActiveWorkspaceInStore])

  // Keyboard shortcuts
  useKeyboard({
    [WORKSPACE_KEYS.ground]: () => setActiveWorkspace('ground'),
    [WORKSPACE_KEYS.brief]: () => setActiveWorkspace('brief'),
    [WORKSPACE_KEYS.shape]: () => setActiveWorkspace('shape'),
    [WORKSPACE_KEYS.ship]: () => setActiveWorkspace('ship'),
  }, [])

  const handleSettings = useCallback(() => {
    console.log('TODO: Open settings modal')
  }, [])

  const handleViewJobResult = useCallback((job: Job) => {
    // Navigate to source workspace if different
    if (job.sourceWorkspace && job.sourceWorkspace !== activeWorkspace) {
      setActiveWorkspace(job.sourceWorkspace as Workspace)
    }
    // The workspace component will handle showing the result based on the job
    // For now, just navigate - workspaces can subscribe to job store for auto-open
  }, [activeWorkspace])

  return (
    <div className="h-screen flex flex-col bg-background text-foreground overflow-hidden">
      {/* Top bar with workspace tabs */}
      <TopBar
        active={activeWorkspace}
        onSwitch={setActiveWorkspace}
        onSettings={handleSettings}
        onViewJobResult={handleViewJobResult}
        activeRuns={0}
        openObservations={3}
      />

      {/* Workspace content */}
      <main className="flex-1 min-h-0">
        {activeWorkspace === 'ground' && <GroundWorkspace />}
        {activeWorkspace === 'brief' && <BriefWorkspace />}
        {activeWorkspace === 'shape' && <ShapeWorkspace />}
        {activeWorkspace === 'ship' && <ShipWorkspace />}
      </main>
    </div>
  )
}

export default App

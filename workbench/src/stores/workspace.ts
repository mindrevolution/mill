import { useState, useCallback } from 'react'
import type { Workspace } from '@/types'

// Simple store using React state (can upgrade to zustand later)
export function useWorkspaceStore() {
  const [activeWorkspace, setActiveWorkspace] = useState<Workspace>('shape')
  const [projectPath, setProjectPath] = useState<string | null>(null)
  const [projectName, setProjectName] = useState<string>('mill')

  const switchWorkspace = useCallback((workspace: Workspace) => {
    setActiveWorkspace(workspace)
  }, [])

  return {
    activeWorkspace,
    switchWorkspace,
    projectPath,
    setProjectPath,
    projectName,
    setProjectName,
  }
}

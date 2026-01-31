import { useEffect, useCallback } from 'react'

type KeyHandler = (e: KeyboardEvent) => void

interface KeyBindings {
  [key: string]: KeyHandler
}

export function useKeyboard(bindings: KeyBindings, deps: unknown[] = []) {
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    // Build key string
    const parts: string[] = []
    if (e.metaKey || e.ctrlKey) parts.push('mod')
    if (e.shiftKey) parts.push('shift')
    if (e.altKey) parts.push('alt')
    parts.push(e.key.toLowerCase())

    const keyString = parts.join('+')

    // Check for match
    const handler = bindings[keyString] || bindings[e.key.toLowerCase()]
    if (handler) {
      e.preventDefault()
      handler(e)
    }
  }, [bindings, ...deps])

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])
}

// Workspace keyboard shortcuts
export const WORKSPACE_KEYS = {
  ground: '1',
  brief: '2',
  shape: '3',
  ship: '4',
}

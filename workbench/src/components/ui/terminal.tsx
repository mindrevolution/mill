import {
  useEffect,
  useRef,
  useImperativeHandle,
  forwardRef,
  useCallback,
  useMemo,
} from 'react'
import { Terminal as XTerm } from '@xterm/xterm'
import { FitAddon } from '@xterm/addon-fit'
import { WebLinksAddon } from '@xterm/addon-web-links'
import '@xterm/xterm/css/xterm.css'

// Debounce helper
function debounce<T extends (...args: Parameters<T>) => void>(
  fn: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout>
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => fn(...args), delay)
  }
}

export interface TerminalHandle {
  write: (data: string) => void
  clear: () => void
  focus: () => void
  fit: () => void
  getDimensions: () => { cols: number; rows: number }
}

export interface TerminalProps {
  onData?: (data: string) => void
  onResize?: (cols: number, rows: number) => void
  className?: string
}

export const Terminal = forwardRef<TerminalHandle, TerminalProps>(
  ({ onData, onResize, className }, ref) => {
    const containerRef = useRef<HTMLDivElement>(null)
    const termRef = useRef<XTerm | null>(null)
    const fitAddonRef = useRef<FitAddon | null>(null)

    // Debounced resize callback to avoid flooding PTY with resize events
    const debouncedOnResize = useMemo(
      () => (onResize ? debounce(onResize, 100) : undefined),
      [onResize]
    )

    // Fit the terminal to container
    const fit = useCallback(() => {
      if (fitAddonRef.current && termRef.current) {
        fitAddonRef.current.fit()
        const { cols, rows } = termRef.current
        debouncedOnResize?.(cols, rows)
      }
    }, [debouncedOnResize])

    // Initialize terminal
    useEffect(() => {
      if (!containerRef.current || termRef.current) return

      const term = new XTerm({
        cursorBlink: true,
        fontFamily: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace',
        fontSize: 13,
        lineHeight: 1.2,
        scrollback: 10000,
        // Let xterm.js handle EOL conversion - helps with cursor positioning
        convertEol: true,
        allowProposedApi: true,
        theme: {
          background: '#09090b',
          foreground: '#fafafa',
          cursor: '#ffcc00',
          cursorAccent: '#09090b',
          selectionBackground: '#3f3f46',
          selectionForeground: '#fafafa',
          black: '#09090b',
          red: '#ef4444',
          green: '#22c55e',
          yellow: '#eab308',
          blue: '#3b82f6',
          magenta: '#a855f7',
          cyan: '#06b6d4',
          white: '#fafafa',
          brightBlack: '#71717a',
          brightRed: '#f87171',
          brightGreen: '#4ade80',
          brightYellow: '#facc15',
          brightBlue: '#60a5fa',
          brightMagenta: '#c084fc',
          brightCyan: '#22d3ee',
          brightWhite: '#ffffff',
        },
      })

      const fitAddon = new FitAddon()
      const webLinksAddon = new WebLinksAddon()

      term.loadAddon(fitAddon)
      term.loadAddon(webLinksAddon)

      term.open(containerRef.current)
      fitAddon.fit()

      termRef.current = term
      fitAddonRef.current = fitAddon

      // Report initial size immediately (not debounced)
      onResize?.(term.cols, term.rows)

      // Handle user input
      term.onData((data) => {
        onData?.(data)
      })

      // Handle resize
      const resizeObserver = new ResizeObserver(() => {
        fit()
      })
      resizeObserver.observe(containerRef.current)

      return () => {
        resizeObserver.disconnect()
        term.dispose()
        termRef.current = null
        fitAddonRef.current = null
        // Clear any pending write buffer
        if (writeTimeoutRef.current) {
          clearTimeout(writeTimeoutRef.current)
        }
      }
    }, [onData, onResize, fit])

    // Buffered write to batch rapid updates (reduces flickering from TUI apps)
    const writeBufferRef = useRef<string[]>([])
    const writeTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

    const flushWriteBuffer = useCallback(() => {
      writeTimeoutRef.current = null
      if (writeBufferRef.current.length === 0) return

      if (termRef.current) {
        const data = writeBufferRef.current.join('')
        writeBufferRef.current = []

        // Debug: log data stats periodically
        const hasAltScreen = data.includes('\x1b[?1049h') || data.includes('\x1b[?47h')
        const hasMainScreen = data.includes('\x1b[?1049l') || data.includes('\x1b[?47l')
        const hasClear = data.includes('\x1b[2J') || data.includes('\x1b[H')
        const hasSyncStart = data.includes('\x1b[?2026h')
        const hasSyncEnd = data.includes('\x1b[?2026l')

        // Find any readable text in the data (skip escape sequences)
        const textOnly = data.replace(/\x1b\[[0-9;?]*[a-zA-Z]/g, '').replace(/\x1b\][^\x07]*\x07/g, '').trim()
        const hasText = textOnly.length > 50

        if (hasAltScreen || hasMainScreen || hasClear || hasSyncStart || hasText) {
          console.log('[Terminal] Write:', {
            altScreen: hasAltScreen,
            mainScreen: hasMainScreen,
            clear: hasClear,
            syncStart: hasSyncStart,
            syncEnd: hasSyncEnd,
            dataLen: data.length,
            textLen: textOnly.length,
            // Show text preview if substantial
            textPreview: hasText ? textOnly.slice(0, 300) : undefined,
          })
        }

        termRef.current.write(data)
      } else {
        // Terminal not ready yet, retry after a short delay
        writeTimeoutRef.current = setTimeout(flushWriteBuffer, 50)
      }
    }, [])

    const bufferedWrite = useCallback((data: string) => {
      writeBufferRef.current.push(data)
      // Batch writes within 16ms (~60fps) to reduce flickering
      if (!writeTimeoutRef.current) {
        writeTimeoutRef.current = setTimeout(flushWriteBuffer, 16)
      }
    }, [flushWriteBuffer])

    // Expose imperative methods
    useImperativeHandle(
      ref,
      () => ({
        write: bufferedWrite,
        clear: () => {
          termRef.current?.clear()
        },
        focus: () => {
          termRef.current?.focus()
        },
        fit,
        getDimensions: () => ({
          cols: termRef.current?.cols ?? 80,
          rows: termRef.current?.rows ?? 24,
        }),
      }),
      [fit, bufferedWrite]
    )

    return (
      <div
        ref={containerRef}
        className={className}
        style={{
          width: '100%',
          height: '100%',
          backgroundColor: '#09090b',
        }}
      />
    )
  }
)

Terminal.displayName = 'Terminal'

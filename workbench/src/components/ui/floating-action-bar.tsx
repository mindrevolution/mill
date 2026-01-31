import { Card, CardContent } from '@/components/ui/card'

interface FloatingActionBarProps {
  children: React.ReactNode
}

export function FloatingActionBar({ children }: FloatingActionBarProps) {
  return (
    <div className="absolute bottom-4 left-1/2 -translate-x-1/2 z-10">
      <Card className="bg-card/80 backdrop-blur-md shadow-lg">
        <CardContent className="flex items-center gap-1 px-2 py-1.5">
          {children}
        </CardContent>
      </Card>
    </div>
  )
}

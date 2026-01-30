import './index.css'
import * as React from 'react'
import { Sidebar } from '@/components/layout/sidebar'
import { MessageList } from '@/components/layout/message-list'
import { MessageDetail } from '@/components/layout/message-detail'

function App() {
  const [selectedMessage, setSelectedMessage] = React.useState('1')

  return (
    <div className="h-screen flex bg-background text-foreground overflow-hidden">
      {/* Sidebar */}
      <aside className="w-56 border-r shrink-0">
        <Sidebar />
      </aside>

      {/* Message List */}
      <div className="w-[400px] border-r shrink-0">
        <MessageList
          selectedId={selectedMessage}
          onSelect={setSelectedMessage}
        />
      </div>

      {/* Message Detail */}
      <main className="flex-1 min-w-0">
        <MessageDetail />
      </main>
    </div>
  )
}

export default App

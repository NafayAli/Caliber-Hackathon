import { useEffect, useState } from 'react'

export function OfflineBanner() {
  const [online, setOnline] = useState(navigator.onLine)

  useEffect(() => {
    const handleOnline = () => setOnline(true)
    const handleOffline = () => setOnline(false)

    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  if (online) {
    return null
  }

  return (
    <div
      role="status"
      className="bg-[var(--color-status-expiring)] px-4 py-2 text-center text-sm font-medium text-white"
    >
      You are offline. Some features may be unavailable until your connection returns.
    </div>
  )
}

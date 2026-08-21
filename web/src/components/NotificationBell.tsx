import { Bell, Megaphone } from 'lucide-react'
import { useMemo, useState } from 'react'
import { toast } from 'sonner'
import {
  useBroadcastAnnouncement,
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
  useNotifications,
  useNotifyEmployees,
  type NotificationDto,
} from '../api/notifications'
import { useApproveRenewal, useDeclineRenewal, usePendingRenewals } from '../api/renewals'
import { isManagerOrAdmin, useAuth } from '../contexts/AuthContext'
import { Sheet } from './ios/Sheet'
import { cn } from '../lib/cn'

function formatWhen(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

export function NotificationBell() {
  const { accessLevel } = useAuth()
  const { data, isLoading } = useNotifications()
  const { data: pendingRenewals = [] } = usePendingRenewals()
  const markRead = useMarkNotificationRead()
  const markAllRead = useMarkAllNotificationsRead()
  const broadcast = useBroadcastAnnouncement()
  const [open, setOpen] = useState(false)
  const [showBroadcast, setShowBroadcast] = useState(false)
  const [title, setTitle] = useState('')
  const [message, setMessage] = useState('')

  const pendingRenewalIds = useMemo(
    () => new Set(pendingRenewals.map((request) => request.id)),
    [pendingRenewals],
  )

  const unread = data?.unreadCount ?? 0
  const items = data?.items ?? []
  const canBroadcast = isManagerOrAdmin(accessLevel)
  const canReviewRenewals = isManagerOrAdmin(accessLevel)

  async function handleBroadcast() {
    if (!title.trim() || !message.trim()) return
    try {
      const result = await broadcast.mutateAsync({ title: title.trim(), message: message.trim() })
      toast.success(`Announcement sent to ${result.sent} employee(s)`)
      setShowBroadcast(false)
      setTitle('')
      setMessage('')
    } catch {
      // global handler
    }
  }

  return (
    <>
      <button
        type="button"
        aria-label="Notifications"
        onClick={() => setOpen(true)}
        className="relative rounded-xl p-2 hover:bg-elevated"
      >
        <Bell className="h-5 w-5 text-label" />
        {unread > 0 ? (
          <span className="absolute -right-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-accent px-1 text-[10px] font-bold text-white">
            {unread > 99 ? '99+' : unread}
          </span>
        ) : null}
      </button>

      <Sheet open={open} onOpenChange={setOpen} title="Notifications" className="max-w-lg">
        <div className="mb-4 flex flex-wrap gap-2">
          {unread > 0 ? (
            <button
              type="button"
              onClick={() => markAllRead.mutate()}
              className="rounded-lg bg-elevated px-3 py-1.5 text-xs font-semibold text-label ring-1 ring-separator/50"
            >
              Mark all read
            </button>
          ) : null}
          {canBroadcast ? (
            <button
              type="button"
              onClick={() => setShowBroadcast(true)}
              className="inline-flex items-center gap-1 rounded-lg bg-accent-muted px-3 py-1.5 text-xs font-semibold text-accent"
            >
              <Megaphone className="h-3.5 w-3.5" />
              Broadcast
            </button>
          ) : null}
        </div>

        {isLoading ? (
          <p className="text-sm text-secondary-label">Loading…</p>
        ) : items.length === 0 ? (
          <p className="text-sm text-secondary-label">No notifications yet.</p>
        ) : (
          <ul className="max-h-[min(60vh,480px)] space-y-2 overflow-auto">
            {items.map((item) => (
              <NotificationRow
                key={item.id}
                item={item}
                canReviewRenewals={canReviewRenewals}
                isRenewalPending={
                  item.renewalRequestId != null && pendingRenewalIds.has(item.renewalRequestId)
                }
                onRead={() => {
                  if (!item.isRead) markRead.mutate(item.id)
                }}
              />
            ))}
          </ul>
        )}
      </Sheet>

      <Sheet open={showBroadcast} onOpenChange={setShowBroadcast} title="Broadcast announcement">
        <div className="space-y-4">
          <label className="block text-sm text-secondary-label">
            Title
            <input
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              className="mt-1 min-h-11 w-full rounded-xl bg-elevated px-3 text-label ring-1 ring-separator/50"
            />
          </label>
          <label className="block text-sm text-secondary-label">
            Message
            <textarea
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              rows={4}
              className="mt-1 w-full rounded-xl bg-elevated px-3 py-2 text-label ring-1 ring-separator/50"
            />
          </label>
          <button
            type="button"
            disabled={!title.trim() || !message.trim() || broadcast.isPending}
            onClick={() => void handleBroadcast()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            Send to team
          </button>
        </div>
      </Sheet>
    </>
  )
}

function NotificationRow({
  item,
  canReviewRenewals,
  isRenewalPending,
  onRead,
}: {
  item: NotificationDto
  canReviewRenewals: boolean
  isRenewalPending: boolean
  onRead: () => void
}) {
  const approve = useApproveRenewal()
  const decline = useDeclineRenewal()
  const showRenewalActions =
    canReviewRenewals
    && item.kind === 'RenewalRequest'
    && item.renewalRequestId != null
    && isRenewalPending

  async function handleApprove() {
    if (!item.renewalRequestId) return
    try {
      await approve.mutateAsync({ id: item.renewalRequestId })
      toast.success('Renewal approved')
      onRead()
    } catch {
      // global handler
    }
  }

  async function handleDecline() {
    if (!item.renewalRequestId) return
    try {
      await decline.mutateAsync({ id: item.renewalRequestId })
      toast.success('Renewal declined')
      onRead()
    } catch {
      // global handler
    }
  }

  return (
    <li
      className={cn(
        'rounded-xl px-4 py-3 ring-1 ring-separator/40',
        item.isRead ? 'bg-grouped' : 'bg-accent-muted/20',
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <button type="button" className="min-w-0 flex-1 text-left" onClick={onRead}>
          <div className="font-medium text-label">{item.title}</div>
          <p className="mt-1 text-sm text-secondary-label">{item.message}</p>
          <p className="mt-2 text-xs text-secondary-label">{formatWhen(item.createdAt)}</p>
        </button>
        {!item.isRead ? (
          <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-accent" />
        ) : null}
      </div>
      {showRenewalActions ? (
        <div className="mt-3 flex gap-2">
          <button
            type="button"
            disabled={approve.isPending || decline.isPending}
            onClick={() => void handleApprove()}
            className="min-h-9 flex-1 rounded-lg bg-status-compliant-bg px-3 text-xs font-semibold text-status-compliant disabled:opacity-40"
          >
            Accept
          </button>
          <button
            type="button"
            disabled={approve.isPending || decline.isPending}
            onClick={() => void handleDecline()}
            className="min-h-9 flex-1 rounded-lg bg-status-danger-bg px-3 text-xs font-semibold text-status-danger disabled:opacity-40"
          >
            Reject
          </button>
        </div>
      ) : null}
    </li>
  )
}

export function NotifyEmployeeButton({
  employeeId,
  employeeName,
  context,
}: {
  employeeId: number
  employeeName: string
  context: string
}) {
  const notify = useNotifyEmployees()

  return (
    <button
      type="button"
      disabled={notify.isPending}
      onClick={() =>
        void notify
          .mutateAsync({
            employeeIds: [employeeId],
            title: 'Action required',
            message: `Please review: ${context}`,
            kind: 'Reminder',
          })
          .then(() => toast.success(`Notification sent to ${employeeName}`))
      }
      className="rounded-lg bg-accent-muted px-2.5 py-1 text-xs font-semibold text-accent disabled:opacity-40"
    >
      Notify
    </button>
  )
}

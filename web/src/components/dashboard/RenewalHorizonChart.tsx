import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { RenewalHorizonDto } from '../../api/dashboard'
import { useChartThemeSnapshot } from '../../lib/chartColors'

const DEFAULT_BUCKETS: RenewalHorizonDto[] = [
  { label: 'Next 30 days', count: 0 },
  { label: '31–60 days', count: 0 },
  { label: '61–90 days', count: 0 },
]

export function RenewalHorizonChart({
  data,
}: {
  data: RenewalHorizonDto[]
}) {
  const theme = useChartThemeSnapshot()
  const chartData = data.length > 0 ? data : DEFAULT_BUCKETS

  if (chartData.every((item) => item.count === 0)) {
    return (
      <div className="space-y-3">
        <div className="h-[220px] w-full">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={theme.grid} />
              <XAxis
                dataKey="label"
                tick={{ fill: theme.label, fontSize: 11 }}
                interval={0}
                angle={-12}
                textAnchor="end"
                height={56}
              />
              <YAxis allowDecimals={false} tick={{ fill: theme.label, fontSize: 12 }} />
              <Bar dataKey="count" fill={theme.accent} radius={[6, 6, 0, 0]} maxBarSize={48} />
            </BarChart>
          </ResponsiveContainer>
        </div>
        <p className="text-center text-xs text-secondary-label">No upcoming renewals in the next 90 days.</p>
      </div>
    )
  }

  return (
    <div className="h-[240px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={theme.grid} />
          <XAxis
            dataKey="label"
            tick={{ fill: theme.label, fontSize: 11 }}
            interval={0}
            angle={-12}
            textAnchor="end"
            height={56}
          />
          <YAxis allowDecimals={false} tick={{ fill: theme.label, fontSize: 12 }} />
          <Tooltip
            formatter={(value: number) => [value, 'Renewals']}
            contentStyle={{
              borderRadius: '12px',
              border: `1px solid ${theme.grid}`,
              background: 'var(--color-grouped-bg)',
              color: 'var(--color-label)',
            }}
          />
          <Bar dataKey="count" fill={theme.accent} radius={[6, 6, 0, 0]} maxBarSize={48} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { KindBreakdownDto } from '../../api/dashboard'
import { KIND_LABELS, useChartThemeSnapshot } from '../../lib/chartColors'
import { ChartEmptyState } from './DashboardChartCard'

export function OpenGapsByKindChart({
  data,
}: {
  data: KindBreakdownDto[]
}) {
  const theme = useChartThemeSnapshot()

  const chartData = data
    .filter((item) => item.count > 0)
    .map((item) => ({
      name: KIND_LABELS[item.kind],
      count: item.count,
      kind: item.kind,
      fill: theme.kindColor(item.kind),
    }))

  if (chartData.length === 0) {
    return <ChartEmptyState message="No open gaps — workforce is on track." />
  }

  return (
    <div className="h-[220px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={theme.grid} />
          <XAxis dataKey="name" tick={{ fill: theme.label, fontSize: 12 }} />
          <YAxis allowDecimals={false} tick={{ fill: theme.label, fontSize: 12 }} />
          <Tooltip
            formatter={(value: number) => [value, 'Open gaps']}
            contentStyle={{
              borderRadius: '12px',
              border: `1px solid ${theme.grid}`,
              background: 'var(--color-grouped-bg)',
              color: 'var(--color-label)',
            }}
          />
          <Bar dataKey="count" radius={[6, 6, 0, 0]} maxBarSize={56}>
            {chartData.map((entry) => (
              <Cell key={entry.kind} fill={entry.fill} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

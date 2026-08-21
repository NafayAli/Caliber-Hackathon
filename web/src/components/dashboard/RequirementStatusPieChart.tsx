import {
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
} from 'recharts'
import type { StatusBreakdownDto } from '../../api/dashboard'
import type { ReadinessStatus } from '../StatusChip'
import {
  STATUS_LABELS,
  useChartThemeSnapshot,
} from '../../lib/chartColors'
import { ChartEmptyState } from './DashboardChartCard'

interface ChartSlice {
  name: string
  value: number
  status: ReadinessStatus
  fill: string
}

export function RequirementStatusPieChart({
  data,
}: {
  data: StatusBreakdownDto[]
}) {
  const theme = useChartThemeSnapshot()

  const slices: ChartSlice[] = data
    .filter((item) => item.count > 0)
    .map((item) => ({
      name: STATUS_LABELS[item.status],
      value: item.count,
      status: item.status,
      fill: theme.statusColor(item.status),
    }))

  const total = slices.reduce((sum, item) => sum + item.value, 0)

  if (total === 0) {
    return <ChartEmptyState message="No requirement data yet." />
  }

  return (
    <div className="relative h-[240px] w-full">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie
            data={slices}
            dataKey="value"
            nameKey="name"
            cx="50%"
            cy="50%"
            innerRadius={52}
            outerRadius={78}
            paddingAngle={2}
          >
            {slices.map((slice) => (
              <Cell key={slice.status} fill={slice.fill} stroke="transparent" />
            ))}
          </Pie>
          <Tooltip
            formatter={(value: number, name: string) => [`${value}`, name]}
            contentStyle={{
              borderRadius: '12px',
              border: `1px solid ${theme.grid}`,
              background: 'var(--color-grouped-bg)',
              color: 'var(--color-label)',
            }}
          />
          <Legend
            verticalAlign="bottom"
            iconType="circle"
            iconSize={8}
            wrapperStyle={{ fontSize: '12px', color: theme.label }}
          />
        </PieChart>
      </ResponsiveContainer>
      <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center pb-8">
        <span className="text-2xl font-semibold tabular-nums text-label">{total}</span>
        <span className="text-xs text-secondary-label">requirements</span>
      </div>
    </div>
  )
}

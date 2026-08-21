import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { LocationComplianceDto } from '../../api/dashboard'
import { useChartThemeSnapshot } from '../../lib/chartColors'
import { ChartEmptyState } from './DashboardChartCard'

export function LocationComplianceChart({
  data,
}: {
  data: LocationComplianceDto[]
}) {
  const theme = useChartThemeSnapshot()

  if (data.length === 0) {
    return <ChartEmptyState message="No location data available." />
  }

  const chartData = data.map((location) => ({
    name: location.locationName,
    compliance: Number(location.compliancePercent),
    employees: location.employeeCount,
  }))

  const chartHeight = Math.max(220, chartData.length * 44)

  return (
    <div className="w-full" style={{ height: chartHeight }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart
          data={chartData}
          layout="vertical"
          margin={{ top: 4, right: 16, left: 4, bottom: 4 }}
        >
          <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke={theme.grid} />
          <XAxis
            type="number"
            domain={[0, 100]}
            tick={{ fill: theme.label, fontSize: 12 }}
            tickFormatter={(value: number) => `${value}%`}
          />
          <YAxis
            type="category"
            dataKey="name"
            width={100}
            tick={{ fill: theme.label, fontSize: 12 }}
          />
          <Tooltip
            formatter={(value: number) => [`${value}%`, 'Compliance']}
            labelFormatter={(label: string, payload) => {
              const employees = payload?.[0]?.payload?.employees
              return employees != null ? `${label} · ${employees} employees` : label
            }}
            contentStyle={{
              borderRadius: '12px',
              border: `1px solid ${theme.grid}`,
              background: 'var(--color-grouped-bg)',
              color: 'var(--color-label)',
            }}
          />
          <Bar dataKey="compliance" fill={theme.accent} radius={[0, 6, 6, 0]} maxBarSize={24} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

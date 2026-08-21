import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute, RequireRole } from './components/ProtectedRoute'
import { AppShell } from './layouts/AppShell'
import { AboutPage } from './pages/AboutPage'
import { CertificationsPage } from './pages/CertificationsPage'
import { DashboardPage } from './pages/DashboardPage'
import { EmployeeListPage } from './pages/EmployeeListPage'
import { EmployeeProfilePage } from './pages/EmployeeProfilePage'
import { ExpirationsPage } from './pages/ExpirationsPage'
import { LoginPage } from './pages/LoginPage'
import { MyRequirementsPage } from './pages/MyRequirementsPage'
import { ProfilePage } from './pages/ProfilePage'
import { ReportsPage } from './pages/ReportsPage'
import { RolesPage } from './pages/RolesPage'
import { SettingsPage } from './pages/SettingsPage'
import { SignupPage } from './pages/SignupPage'
import { SkillsPage } from './pages/SkillsPage'
import { TrainingPage } from './pages/TrainingPage'
import { UsersPage } from './pages/UsersPage'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/signup" element={<SignupPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="employees" element={<EmployeeListPage />} />
          <Route path="employees/:id" element={<EmployeeProfilePage />} />
          <Route path="certifications" element={<CertificationsPage />} />
          <Route path="training" element={<TrainingPage />} />
          <Route path="skills" element={<SkillsPage />} />
          <Route
            path="roles"
            element={(
              <RequireRole adminOnly>
                <RolesPage />
              </RequireRole>
            )}
          />
          <Route path="expirations" element={<ExpirationsPage />} />
          <Route path="my" element={<MyRequirementsPage />} />
          <Route
            path="users"
            element={(
              <RequireRole managerOrAdmin>
                <UsersPage />
              </RequireRole>
            )}
          />
          <Route
            path="reports"
            element={(
              <RequireRole managerOrAdmin>
                <ReportsPage />
              </RequireRole>
            )}
          />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="about" element={<AboutPage />} />
          <Route
            path="settings"
            element={(
              <RequireRole managerOrAdmin>
                <SettingsPage />
              </RequireRole>
            )}
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}

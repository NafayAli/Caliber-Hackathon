import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { PersonaProvider } from './contexts/PersonaContext'
import { AppRoutes } from './routes'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <PersonaProvider>
          <AppRoutes />
        </PersonaProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}

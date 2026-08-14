import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

/**
 * Wrap any route with <ProtectedRoute> to require login.
 * If not logged in, redirects to /login.
 */
export default function ProtectedRoute({ children }) {
  const { isLoggedIn, loading } = useAuth()

  // Still reading localStorage — show nothing yet
  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="w-8 h-8 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return isLoggedIn ? children : <Navigate to="/login" replace />
}

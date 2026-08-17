import { createContext, useContext, useState, useEffect } from 'react'

const AuthContext = createContext(null)

/**
 * AuthProvider wraps the whole app.
 * It stores the JWT token + user info in localStorage so the
 * session survives a page refresh.
 */
export function AuthProvider({ children }) {
  const [user,    setUser]    = useState(null)
  const [token,   setToken]   = useState(null)
  const [loading, setLoading] = useState(true)   // true while reading localStorage

  // On first load, restore session from localStorage
  useEffect(() => {
    const savedToken = localStorage.getItem('mpkv_token')
    const savedUser  = localStorage.getItem('mpkv_user')
    if (savedToken && savedUser) {
      setToken(savedToken)
      setUser(JSON.parse(savedUser))
    }
    setLoading(false)
  }, [])

  const login = (tokenValue, userValue) => {
    setToken(tokenValue)
    setUser(userValue)
    localStorage.setItem('mpkv_token', tokenValue)
    localStorage.setItem('mpkv_user',  JSON.stringify(userValue))
  }

  // Update specific user fields (e.g. photoPath after upload) without re-login
  const updateUser = (fields) => {
    setUser(prev => {
      const updated = { ...prev, ...fields }
      localStorage.setItem('mpkv_user', JSON.stringify(updated))
      return updated
    })
  }

  const logout = () => {
    setToken(null)
    setUser(null)
    localStorage.removeItem('mpkv_token')
    localStorage.removeItem('mpkv_user')
    sessionStorage.clear()
  }

  return (
    <AuthContext.Provider value={{ user, token, loading, login, logout, updateUser, isLoggedIn: !!token }}>
      {children}
    </AuthContext.Provider>
  )
}

// Custom hook — use this in any component
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}

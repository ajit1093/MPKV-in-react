import Navbar from './Navbar'

/**
 * CandidateLayout — wraps all protected candidate pages.
 *
 * Renders:
 *   1. Brand header (university name + candidate photo)   ← inside Navbar.jsx
 *   2. Dark navbar with dropdowns                         ← inside Navbar.jsx
 *   3. Page content
 *
 * Individual pages must NOT import or render <Navbar /> themselves.
 */
export default function CandidateLayout({ children }) {
  return (
    <div className="min-h-screen bg-gray-50 flex flex-col font-sans">
      <Navbar />
      <main className="flex-1">
        {children}
      </main>
    </div>
  )
}

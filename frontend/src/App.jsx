import { Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import PublicLayout from './components/PublicLayout'
import CandidateLayout from './components/CandidateLayout'

// Pages
import Home             from './pages/Home'
import Login            from './pages/Login'
import Registration     from './pages/Registration'
import RegistrationInfo from './pages/RegistrationInfo'
import ForgotLoginId    from './pages/ForgotLoginId'
import ForgotPassword   from './pages/ForgotPassword'
import ResetPassword    from './pages/ResetPassword'
import Dashboard        from './pages/candidate/Dashboard'
import Personal         from './pages/candidate/Personal'
import Address          from './pages/candidate/Address'
import Category         from './pages/candidate/Category'
import Sports           from './pages/candidate/Sports'
import Qualification    from './pages/candidate/Qualification'
import Documents        from './pages/candidate/Documents'
import Fee              from './pages/candidate/Fee'
import PaymentSuccess   from './pages/candidate/PaymentSuccess'
import PaymentFailed    from './pages/candidate/PaymentFailed'
import Shortlist         from './pages/candidate/Shortlist'
import SetPreferences    from './pages/candidate/SetPreferences'
import PhotoSign         from './pages/candidate/PhotoSign'

// Simple coming-soon for unbuilt public pages
function PublicComingSoon({ title }) {
  return (
    <PublicLayout>
      <div className="min-h-[50vh] flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <i className="fas fa-tools text-emerald-600 text-2xl" />
          </div>
          <h2 className="text-xl font-bold text-gray-700 mb-2">{title}</h2>
          <p className="text-gray-400 text-sm">This page is coming soon.</p>
          <a href="/" className="inline-block mt-4 text-emerald-600 hover:underline text-sm">
            ← Back to Home
          </a>
        </div>
      </div>
    </PublicLayout>
  )
}

// Simple coming-soon for unbuilt candidate pages
function CandidateComingSoon({ page }) {
  return (
    <div className="min-h-[50vh] flex items-center justify-center">
      <div className="text-center">
        <div className="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <i className="fas fa-tools text-emerald-600 text-2xl" />
        </div>
        <h2 className="text-xl font-bold text-gray-700 mb-2">{page}</h2>
        <p className="text-gray-400 text-sm">This page is coming soon.</p>
        <a href="/candidate/dashboard" className="inline-block mt-4 text-emerald-600 hover:underline text-sm">
          ← Back to Dashboard
        </a>
      </div>
    </div>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>

        {/* ── Public routes ───────────────────────────────────────────── */}
        <Route path="/"               element={<Home />} />
        <Route path="/login"          element={<Login />} />
        <Route path="/register"       element={<Registration />} />
        <Route path="/register/info"  element={<RegistrationInfo />} />
        <Route path="/search-college" element={<PublicComingSoon title="Search Colleges"  />} />
        <Route path="/allotment"      element={<PublicComingSoon title="Allotment List"   />} />
        <Route path="/about"          element={<PublicComingSoon title="About Us"         />} />
        <Route path="/terms"          element={<PublicComingSoon title="Terms & Conditions" />} />
        <Route path="/privacy"        element={<PublicComingSoon title="Privacy Policy"   />} />
        <Route path="/refund"         element={<PublicComingSoon title="Refund & Cancellation Policy" />} />
        <Route path="/disclaimer"     element={<PublicComingSoon title="Disclaimer"       />} />
        <Route path="/forgot-login-id"   element={<ForgotLoginId />} />
        <Route path="/forgot-password"   element={<ForgotPassword />} />
        <Route path="/reset-password"    element={<ResetPassword />} />

        {/* ── Candidate protected routes ──────────────────────────────── */}
        <Route path="/candidate/dashboard"
          element={<ProtectedRoute><CandidateLayout><Dashboard /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/personal"
          element={<ProtectedRoute><CandidateLayout><Personal /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/address"
          element={<ProtectedRoute><CandidateLayout><Address /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/category"
          element={<ProtectedRoute><CandidateLayout><Category /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/qualification"
          element={<ProtectedRoute><CandidateLayout><Qualification /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/sports"
          element={<ProtectedRoute><CandidateLayout><Sports /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/shortlist"
          element={<ProtectedRoute><CandidateLayout><Shortlist /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/preferences"
          element={<ProtectedRoute><CandidateLayout><SetPreferences /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/photo-sign"
          element={<ProtectedRoute><CandidateLayout><PhotoSign /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/documents"
          element={<ProtectedRoute><CandidateLayout><Documents /></CandidateLayout></ProtectedRoute>} />
        <Route path="/candidate/fee"
          element={<ProtectedRoute><CandidateLayout><Fee /></CandidateLayout></ProtectedRoute>} />
        <Route path="/payment-success"
          element={<PaymentSuccess />} />
        <Route path="/payment-failed"
          element={<PaymentFailed />} />
        <Route path="/candidate/summary"
          element={<ProtectedRoute><CandidateLayout><CandidateComingSoon page="Application Summary" /></CandidateLayout></ProtectedRoute>} />

        {/* 404 fallback */}
        <Route path="*" element={<Navigate to="/" replace />} />

      </Routes>
    </AuthProvider>
  )
}

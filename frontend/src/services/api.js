import axios from 'axios'

/**
 * Base axios instance.
 * All API calls go through here — the proxy in vite.config.js
 * forwards /api/* to https://localhost:7000/api/*
 */
const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
})

// ── Request interceptor — attach JWT token automatically ─────────────────────
api.interceptors.request.use(config => {
  const token = localStorage.getItem('mpkv_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// ── Response interceptor — handle 401 globally ───────────────────────────────
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      // Token expired or invalid — clear storage and redirect to login
      localStorage.removeItem('mpkv_token')
      localStorage.removeItem('mpkv_user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// ── Home endpoints ────────────────────────────────────────────────────────────
export const homeApi = {
  getHomeData: (regionId = 1) =>
    api.get(`/home?regionId=${regionId}`)
}

// ── Registration endpoints ────────────────────────────────────────────────────
export const registrationApi = {
  // Is registration currently open?
  checkStatus: () =>
    api.get('/registration/check-status'),

  // Courses, genders, security questions
  getMasters: () =>
    api.get('/registration/masters'),

  // Register new candidate
  register: (data) =>
    api.post('/registration/register', data),

  // Get info for confirmation page
  getInfo: (loginId) =>
    api.get(`/registration/info?loginId=${encodeURIComponent(loginId)}`)
}

// ── Application Form endpoints ────────────────────────────────────────────────
export const applicationFormApi = {
  // Personal
  getPersonalMasters : ()       => api.get('/applicationform/masters/personal'),
  getPersonal        : ()       => api.get('/applicationform/personal'),
  savePersonal       : (data)   => api.post('/applicationform/personal', data),

  // Address
  getAddressMasters  : ()       => api.get('/applicationform/masters/address'),
  getAddress         : ()       => api.get('/applicationform/address'),
  saveAddress        : (data)   => api.post('/applicationform/address', data),

  // Category & Other Reservation
  getCategoryMasters : ()       => api.get('/applicationform/masters/category'),
  getCategory        : ()       => api.get('/applicationform/category'),
  saveCategory       : (data)   => api.post('/applicationform/category', data),

  // Sports Details
  getSportsMasters   : ()       => api.get('/applicationform/masters/sports'),
  getSports          : ()       => api.get('/applicationform/sports'),
  saveSports         : (data)   => api.post('/applicationform/sports', data),

  // Shortlist Options
  getAvailableOptions  : ()           => api.get('/applicationform/options/available'),
  getShortlistedOptions: ()           => api.get('/applicationform/options/shortlisted'),
  addOption            : (data)       => api.post('/applicationform/options/add', data),
  removeOption         : (collegeId)  => api.delete(`/applicationform/options/remove/${collegeId}`),
  saveShortlist        : ()           => api.post('/applicationform/options/save'),

  // Set Preferences
  getPreferencedOptions: ()     => api.get('/applicationform/options/preferenced'),
  savePreferences      : (data) => api.post('/applicationform/options/preferences', data),
  resetPreferences     : ()     => api.post('/applicationform/options/preferences/reset'),

  // Photo & Signature
  getPhotoSign       : ()     => api.get('/applicationform/photo-sign'),
  uploadPhoto        : (file) => {
    const fd = new FormData(); fd.append('file', file)
    return api.post('/applicationform/upload-photo', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  },
  uploadSign         : (file) => {
    const fd = new FormData(); fd.append('file', file)
    return api.post('/applicationform/upload-sign', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  },
  savePhotoSign      : ()     => api.post('/applicationform/photo-sign/save'),

  // Qualification
  getQualificationMasters: ()     => api.get('/applicationform/masters/qualification'),
  getQualification       : ()     => api.get('/applicationform/qualification'),
  saveQualification      : (data) => api.post('/applicationform/qualification', data),
}

// ── Account Recovery endpoints ────────────────────────────────────────────────
export const accountApi = {
  // Security questions dropdown
  getMasters: () =>
    api.get('/account/masters'),

  // Forgot Login ID — Step 1: verify name+mobile → send OTP to mobile
  forgotLoginIdSendOtp: (data) =>
    api.post('/account/forgot-login-id/send-otp', data),

  // Forgot Login ID — Step 2: verify OTP → reveal Login ID
  forgotLoginIdVerifyOtp: (data) =>
    api.post('/account/forgot-login-id/verify-otp', data),

  // Reset by Security Question → returns resetToken
  resetBySecurityQuestion: (data) =>
    api.post('/account/reset-password-by-security-question', data),

  // Send OTP to mobile (verify loginId + mobile exist first)
  sendOtpMobile: (data) =>
    api.post('/account/send-otp/mobile', data),

  // Send OTP to email (verify loginId + email exist first)
  sendOtpEmail: (data) =>
    api.post('/account/send-otp/email', data),

  // Verify OTP (mobile or email) → returns resetToken
  verifyOtp: (data) =>
    api.post('/account/verify-otp', data),

  // Final reset — new password + confirmPassword + resetToken
  resetPassword: (data) =>
    api.post('/account/reset-password', data),
}

// ── Auth endpoints ────────────────────────────────────────────────────────────
export const authApi = {
  login: (userLoginID, userPassword) =>
    api.post('/auth/login', { userLoginID, userPassword }),

  me: () =>
    api.get('/auth/me')
}

// ── Dashboard endpoints ───────────────────────────────────────────────────────
export const dashboardApi = {
  getDashboard: () =>
    api.get('/dashboard'),

  getProgress: () =>
    api.get('/dashboard/progress')
}

export default api

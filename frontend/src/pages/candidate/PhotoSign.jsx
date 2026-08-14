import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { applicationFormApi } from '../../services/api'
import { useAuth } from '../../context/AuthContext'

const API_BASE = 'http://localhost:7001'

export default function PhotoSign() {
  const navigate             = useNavigate()
  const { user, updateUser } = useAuth()

  const [photoURL,       setPhotoURL]       = useState('')
  const [signURL,        setSignURL]        = useState('')
  const [pageLoading,    setPageLoading]    = useState(true)
  const [applicationId,  setApplicationId]  = useState('')

  const [photoFile,      setPhotoFile]      = useState(null)
  const [signFile,       setSignFile]       = useState(null)
  const [uploadingPhoto, setUploadingPhoto] = useState(false)
  const [uploadingSign,  setUploadingSign]  = useState(false)
  const [photoError,     setPhotoError]     = useState('')
  const [signError,      setSignError]      = useState('')
  const [photoSuccess,   setPhotoSuccess]   = useState('')
  const [signSuccess,    setSignSuccess]    = useState('')
  const [saving,         setSaving]         = useState(false)
  const [error,          setError]          = useState('')

  const photoInputRef = useRef(null)
  const signInputRef  = useRef(null)

  const bothUploaded = photoURL.length > 0 && signURL.length > 0

  // Build display URL — relative paths get API_BASE prefix
  const displayUrl = url =>
    !url ? '' : (url.startsWith('http') ? url : `${API_BASE}${url}`)

  // Load from DB on every mount — same as GetPhotoAndSign(input) on page load
  useEffect(() => {
    setApplicationId(user?.userLoginID ?? '')
    applicationFormApi.getPhotoSign()
      .then(res => {
        // Always read URLs regardless of found flag
        const pUrl = res.data?.photoUploadedURL ?? ''
        const sUrl = res.data?.signUploadedURL  ?? ''
        setPhotoURL(pUrl)
        setSignURL(sUrl)
        // If photo is in DB, sync navbar profile icon
        if (pUrl && updateUser) updateUser({ photoPath: pUrl })
      })
      .catch(() => setError('Failed to load page data. Please refresh.'))
      .finally(() => setPageLoading(false))
  }, [])

  const validateFile = (file, type) => {
    const ext = file.name.split('.').pop().toLowerCase()
    if (ext !== 'jpg' && ext !== 'jpeg')
      return type === 'photo' ? 'Photograph Format should be jpg/jpeg.' : 'Signature Format should be jpg/jpeg.'
    const sizeKB = file.size / 1024
    if (type === 'photo' && (sizeKB < 10 || sizeKB > 100))
      return 'Photograph Size must be greater than 10 KB and less than 100 KB.'
    if (type === 'sign' && (sizeKB < 5 || sizeKB > 50))
      return 'Signature Size must be greater than 5 KB and less than 50 KB.'
    return ''
  }

  const handlePhotoSelect = e => {
    const file = e.target.files?.[0]; if (!file) return
    const err = validateFile(file, 'photo')
    if (err) { setPhotoError(err); setPhotoFile(null); return }
    setPhotoFile(file); setPhotoError(''); setPhotoSuccess('')
  }

  const handleSignSelect = e => {
    const file = e.target.files?.[0]; if (!file) return
    const err = validateFile(file, 'sign')
    if (err) { setSignError(err); setSignFile(null); return }
    setSignFile(file); setSignError(''); setSignSuccess('')
  }

  const handleUploadPhoto = async () => {
    if (!photoFile) { setPhotoError('Please select a photograph to upload.'); return }
    setUploadingPhoto(true); setPhotoError(''); setPhotoSuccess('')
    try {
      const res = await applicationFormApi.uploadPhoto(photoFile)
      if (res.data.success) {
        const url = res.data.uploadedURL
        setPhotoURL(url)
        setPhotoSuccess(res.data.message)
        setPhotoFile(null)
        if (photoInputRef.current) photoInputRef.current.value = ''
        // Update navbar profile icon immediately
        if (updateUser) updateUser({ photoPath: url })
      } else { setPhotoError(res.data.message || 'Upload failed.') }
    } catch (err) {
      setPhotoError(err.response?.data?.message ?? 'Photograph upload failed. Please try again.')
    } finally { setUploadingPhoto(false) }
  }

  const handleUploadSign = async () => {
    if (!signFile) { setSignError('Please select a signature to upload.'); return }
    setUploadingSign(true); setSignError(''); setSignSuccess('')
    try {
      const res = await applicationFormApi.uploadSign(signFile)
      if (res.data.success) {
        const url = res.data.uploadedURL
        setSignURL(url)
        setSignSuccess(res.data.message)
        setSignFile(null)
        if (signInputRef.current) signInputRef.current.value = ''
      } else { setSignError(res.data.message || 'Upload failed.') }
    } catch (err) {
      setSignError(err.response?.data?.message ?? 'Signature upload failed. Please try again.')
    } finally { setUploadingSign(false) }
  }

  const handleProceed = async () => {
    if (!bothUploaded) { setError('Please upload both Photograph and Signature before proceeding.'); return }
    setSaving(true); setError('')
    try {
      const res = await applicationFormApi.savePhotoSign()
      if (res.data.success) navigate('/candidate/documents')
      else setError(res.data.message || 'Failed to save.')
    } catch (err) {
      setError(err.response?.data?.message ?? 'Failed to proceed.')
    } finally { setSaving(false) }
  }
  const [photoError,     setPhotoError]     = useState('')
  const [signError,      setSignError]      = useState('')
  const [photoSuccess,   setPhotoSuccess]   = useState('')
  const [signSuccess,    setSignSuccess]    = useState('')
  const [saving,         setSaving]         = useState(false)
  const [error,          setError]          = useState('')

  const bothUploaded = photoURL.length > 0 && signURL.length > 0

  const photoInputRef = useRef(null)
  const signInputRef  = useRef(null)

  useEffect(() => {
    setApplicationId(user?.userLoginID ?? '')
    applicationFormApi.getPhotoSign()
  if (pageLoading) return (
    <div style={{ display:'flex',alignItems:'center',justifyContent:'center',minHeight:'60vh' }}>
      <div style={{ textAlign:'center' }}>
        <div className="w-10 h-10 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin mx-auto mb-3"/>
        <p style={{ color:'#64748b',fontSize:14 }}>Loading...</p>
      </div>
    </div>
  )

  const V = {
    navy:'#14212e', primary:'#059669', primaryDark:'#047857',
    teal:'#0d9488', tealLight:'#f0fdfb', tealBorder:'#ccfbf1',
    border:'#e2e8f0', borderLight:'#f1f5f9',
    textPrimary:'#0f172a', textSecond:'#64748b', textLight:'#94a3b8',
    danger:'#ef4444', bg:'#f5f6fa',
  }

  const steps = [
    { label:'Application Form',               done:true,  active:false },
    { label:'College Selection & Preference', done:true,  active:false },
    { label:'Documents Upload',               done:false, active:true  },
    { label:'Fee Payment',                    done:false, active:false },
    { label:'Lock Form',                      done:false, active:false },
  ]

  return (
    <div style={{ fontFamily:'inherit', background:V.bg, minHeight:'100vh', paddingBottom:40 }}>

      {/* top info bar */}
      <div style={{ background:'#fff', borderBottom:`1px solid ${V.border}`, padding:'10px 24px', display:'flex', alignItems:'center', flexWrap:'wrap', gap:'6px 32px' }}>
        <span style={{ fontSize:12, color:V.textSecond, fontWeight:600, letterSpacing:'0.05em', textTransform:'uppercase' }}>Application ID</span>
        <span style={{ fontSize:15, fontWeight:700, color:V.primary }}>{applicationId||'—'}</span>
      </div>

      {/* step-bar */}
      <div style={{ display:'flex', alignItems:'center', gap:4, padding:'20px 24px 0', flexWrap:'wrap' }}>
        {steps.map((s,i,arr)=>(
          <div key={i} style={{ display:'flex', alignItems:'center', gap:4 }}>
            <div style={{ display:'flex', alignItems:'center', gap:6, padding:'6px 14px', borderRadius:20, fontSize:12.5, fontWeight:600, background:s.active?V.primary:s.done?V.tealLight:V.borderLight, color:s.active?'#fff':s.done?V.teal:V.textSecond, border:`1px solid ${s.active?V.primary:s.done?V.tealBorder:V.border}` }}>
              {s.done&&<i className="fas fa-check" style={{ fontSize:9 }}/>}
              {s.active&&<i className="fas fa-circle" style={{ fontSize:8 }}/>}
              {s.label}
            </div>
            {i<arr.length-1&&<span style={{ color:V.textLight, fontSize:12 }}>›</span>}
          </div>
        ))}
      </div>

      <div style={{ padding:'20px 24px 24px' }}>
        {error&&<div style={{ background:'#fef2f2', border:'1px solid #fecaca', color:'#dc2626', borderRadius:8, padding:'10px 16px', marginBottom:16, fontSize:13, display:'flex', alignItems:'center', gap:8 }}><i className="fas fa-exclamation-circle"/> {error}</div>}

        <div style={{ background:'#fff', border:`1px solid ${V.border}`, borderRadius:14, overflow:'hidden', boxShadow:'0 2px 10px rgba(0,0,0,0.06)' }}>

          {/* header */}
          <div style={{ background:V.navy, padding:'16px 24px' }}>
            <h3 style={{ fontSize:16, fontWeight:700, color:'#fff', margin:0 }}>Upload Photo &amp; Signature</h3>
          </div>

          {/* instructions */}
          <div style={{ background:V.tealLight, borderBottom:`1px solid ${V.tealBorder}`, padding:'14px 24px' }}>
            <div style={{ fontSize:12, fontWeight:700, color:V.teal, marginBottom:8, textTransform:'uppercase', letterSpacing:'.04em' }}>
              <i className="fas fa-info-circle"/> Instructions
            </div>
            <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:'4px 40px' }} className="instr-grid">
              <div style={{ color:V.textPrimary, fontSize:12.5, lineHeight:1.8 }}>
                <strong>Photograph:</strong>
                <ul style={{ margin:'2px 0 0', paddingLeft:18 }}>
                  <li>Format: <strong>JPG / JPEG only</strong></li>
                  <li>Size: <strong>10 KB – 100 KB</strong></li>
                </ul>
              </div>
              <div style={{ color:V.textPrimary, fontSize:12.5, lineHeight:1.8 }}>
                <strong>Signature:</strong>
                <ul style={{ margin:'2px 0 0', paddingLeft:18 }}>
                  <li>Format: <strong>JPG / JPEG only</strong></li>
                  <li>Size: <strong>5 KB – 50 KB</strong></li>
                </ul>
              </div>
            </div>
          </div>

          {/* upload panels */}
          <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:24, padding:24 }} className="upload-grid">
            <UploadPanel title="Photograph" icon="fas fa-user-circle"
              fileURL={photoURL} displayURL={displayUrl(photoURL)}
              file={photoFile} inputRef={photoInputRef}
              uploading={uploadingPhoto} error={photoError} success={photoSuccess}
              acceptLabel="JPG/JPEG • 10 KB – 100 KB"
              onSelect={handlePhotoSelect} onUpload={handleUploadPhoto} V={V}/>
            <UploadPanel title="Signature" icon="fas fa-signature"
              fileURL={signURL} displayURL={displayUrl(signURL)}
              file={signFile} inputRef={signInputRef}
              uploading={uploadingSign} error={signError} success={signSuccess}
              acceptLabel="JPG/JPEG • 5 KB – 50 KB"
              onSelect={handleSignSelect} onUpload={handleUploadSign} V={V}/>
          </div>

          {/* footer */}
          <div style={{ position:'relative', display:'flex', alignItems:'center', padding:'16px 24px', borderTop:`1px solid ${V.borderLight}`, background:'#f8fafc', borderRadius:'0 0 14px 14px', flexWrap:'wrap', gap:12 }}>
            <div style={{ fontSize:12, color:V.textSecond, flex:1 }}>
              <span style={{ color:V.danger }}>*</span> Upload both Photograph and Signature to enable Proceed
            </div>
            <div style={{ display:'flex', gap:10, position:'absolute', left:'50%', transform:'translateX(-50%)' }}>
              <button type="button" onClick={()=>navigate('/candidate/preferences')}
                style={{ background:'transparent', color:V.textPrimary, border:`1.5px solid ${V.border}`, padding:'10px 20px', borderRadius:8, fontSize:13.5, fontWeight:600, cursor:'pointer', display:'flex', alignItems:'center', gap:6, fontFamily:'inherit' }}>
                <i className="fas fa-arrow-left"/> Back
              </button>
              <button type="button" onClick={handleProceed} disabled={!bothUploaded||saving}
                title={!bothUploaded?'Upload both Photograph and Signature first':''}
                style={{ background:(!bothUploaded||saving)?'#d1fae5':V.primary, color:(!bothUploaded||saving)?'#6b7280':'#fff', border:'none', padding:'10px 24px', borderRadius:8, fontSize:13.5, fontWeight:600, cursor:(!bothUploaded||saving)?'not-allowed':'pointer', display:'flex', alignItems:'center', gap:6, fontFamily:'inherit', transition:'all .2s' }}
                onMouseEnter={e=>{ if(bothUploaded&&!saving) e.currentTarget.style.background=V.primaryDark }}
                onMouseLeave={e=>{ if(bothUploaded&&!saving) e.currentTarget.style.background=V.primary }}>
                {saving?<><span className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin inline-block"/>Saving...</>:<>Proceed <i className="fas fa-arrow-right" style={{ fontSize:12 }}/></>}
              </button>
            </div>
            <div style={{ flex:1 }}/>
          </div>
        </div>
      </div>

      <button onClick={()=>window.scrollTo({top:0,behavior:'smooth'})}
        style={{ position:'fixed', bottom:28, right:28, width:44, height:44, borderRadius:'50%', background:'#f97316', color:'#fff', border:'none', fontSize:18, cursor:'pointer', display:'flex', alignItems:'center', justifyContent:'center', boxShadow:'0 4px 12px rgba(249,115,22,0.4)', zIndex:50 }}>
        <i className="fas fa-chevron-up"/>
      </button>

      <style>{`
        @media(max-width:768px){.upload-grid,.instr-grid{grid-template-columns:1fr!important;}}
      `}</style>
    </div>
  )
}

function UploadPanel({ title, icon, fileURL, displayURL, file, inputRef, uploading, error, success, acceptLabel, onSelect, onUpload, V }) {
  // Use displayURL (already has API_BASE prefix) — fall back to dummy only when no URL at all
  const imgSrc = displayURL || null

  return (
    <div style={{ border:`1.5px solid ${V.border}`, borderRadius:12, overflow:'hidden' }}>
      <div style={{ background:V.teal, padding:'10px 16px', display:'flex', alignItems:'center', gap:8 }}>
        <i className={icon} style={{ color:'#fff', fontSize:14 }}/>
        <span style={{ color:'#fff', fontSize:13.5, fontWeight:700 }}>{title}</span>
      </div>
      <div style={{ padding:16 }}>
        <div style={{ textAlign:'center', marginBottom:14 }}>
          {/* Show uploaded image OR placeholder — do NOT fall back to dummy-user.png for sign */}
          {imgSrc ? (
            <img src={imgSrc} alt={title}
              style={{ width:120, height:120, objectFit:'contain', border:`2px solid ${V.primary}`, borderRadius:8, background:'#f8fafc' }}
              onError={e => { e.currentTarget.style.display = 'none'; e.currentTarget.nextSibling.style.display = 'flex' }}
            />
          ) : null}
          {/* Placeholder shown when no image or image fails to load */}
          <div style={{ width:120, height:120, border:`2px dashed ${V.border}`, borderRadius:8, background:'#f8fafc', display: imgSrc ? 'none' : 'flex', alignItems:'center', justifyContent:'center', margin:'0 auto' }}>
            <i className={icon === 'fas fa-signature' ? 'fas fa-pen-nib' : 'fas fa-user'} style={{ fontSize:36, color:V.textLight }}/>
          </div>
          <div style={{ marginTop:6, fontSize:12, fontWeight:600 }}>
            {fileURL
              ? <span style={{ color:V.primary }}><i className="fas fa-check-circle"/> Uploaded</span>
              : <span style={{ color:V.textLight }}><i className="fas fa-times-circle"/> Not Uploaded</span>}
          </div>
        </div>
        <div style={{ display:'flex', gap:8, marginBottom:8 }}>
          <input ref={inputRef} type="file" accept=".jpg,.jpeg" onChange={onSelect}
            style={{ flex:1, fontSize:12, border:`1px solid ${V.border}`, borderRadius:6, padding:'6px 8px', background:'#fff', fontFamily:'inherit' }}/>
          <button type="button" onClick={onUpload} disabled={!file||uploading}
            style={{ background:(!file||uploading)?'#e2e8f0':V.primary, color:(!file||uploading)?'#6b7280':'#fff', border:'none', padding:'7px 14px', borderRadius:6, fontSize:12.5, fontWeight:600, cursor:(!file||uploading)?'not-allowed':'pointer', display:'flex', alignItems:'center', gap:5, whiteSpace:'nowrap', fontFamily:'inherit' }}
            onMouseEnter={e=>{ if(file&&!uploading) e.currentTarget.style.background=V.primaryDark }}
            onMouseLeave={e=>{ if(file&&!uploading) e.currentTarget.style.background=V.primary }}>
            {uploading?<><span className="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin inline-block"/>Uploading...</>:<><i className="fas fa-upload" style={{ fontSize:11 }}/>Upload</>}
          </button>
        </div>
        <p style={{ fontSize:11, color:V.textLight, margin:0 }}><i className="fas fa-info-circle"/> {acceptLabel}</p>
        {error&&<p style={{ fontSize:11.5, color:V.danger, margin:'6px 0 0', display:'flex', alignItems:'center', gap:4 }}><i className="fas fa-exclamation-circle"/> {error}</p>}
        {success&&<p style={{ fontSize:11.5, color:V.primary, margin:'6px 0 0', display:'flex', alignItems:'center', gap:4 }}><i className="fas fa-check-circle"/> {success}</p>}
      </div>
    </div>
  )
}

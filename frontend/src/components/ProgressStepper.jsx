import { useState } from 'react'

/**
 * Reusable Application Progress Stepper component.
 * Mirrors the exact design from the old DashboardCandidate.aspx —
 * dark navy header, horizontal connector line, hover dropdowns.
 *
 * Props:
 *   progress — ApplicationProgressResponse from the API
 */
export default function ProgressStepper({ progress }) {
  if (!progress) return null

  // Determine dot status for a sub-page
  // green  = filled
  // orange = first incomplete in an active round
  // blank  = not yet reached
  const buildRoundDots = (pages, roundIsActive) => {
    let orangeGiven = false
    return pages.map(page => {
      if (page.done) return { ...page, status: 'green' }
      if (roundIsActive && !orangeGiven) {
        orangeGiven = true
        return { ...page, status: 'orange' }
      }
      return { ...page, status: 'blank' }
    })
  }

  const round2 = buildRoundDots([
    { label: 'Personal Info',           done: progress.personalDetails      },
    { label: 'Address',                 done: progress.addressDetails       },
    { label: 'Category & Reservation',  done: progress.categoryDetails      },
    { label: 'Qualification',           done: progress.qualificationDetails },
    { label: 'Sports Details',          done: progress.sportsDetails        },
  ], true)  // always active

  const round3 = buildRoundDots([
    { label: 'Shortlist Options', done: progress.shortlistOptions },
    { label: 'Set Preferences',   done: progress.setPreferences   },
  ], progress.personalInfo === true)  // active only when round 2 done

  const round4 = buildRoundDots([
    { label: 'Photo & Signature',    done: progress.photoAndSign      },
    { label: 'Required Documents',   done: progress.requiredDocuments  },
  ], progress.collegeSelection === true)  // active only when round 3 done

  const steps = [
    {
      number: 1,
      label:  'Registration',
      state:  'done',
      dropdownTitle: null,
      pages: []
    },
    {
      number: 2,
      label:  'Personal info',
      state:  progress.personalInfo
                ? 'done'
                : progress.registration && !progress.collegeSelection
                ? 'active'
                : 'pending',
      dropdownTitle: 'Application Form Pages',
      pages: round2
    },
    {
      number: 3,
      label:  ['College', 'selection &', 'preference'],
      state:  progress.collegeSelection
                ? 'done'
                : progress.personalInfo && !progress.documentUpload
                ? 'active'
                : 'pending',
      dropdownTitle: 'College Preference Pages',
      pages: round3
    },
    {
      number: 4,
      label:  ['Upload', 'documents'],
      state:  progress.documentUpload
                ? 'done'
                : progress.collegeSelection && !progress.feePayment
                ? 'active'
                : 'pending',
      dropdownTitle: 'Document Pages',
      pages: round4
    },
    {
      number: 5,
      label:  'Fee payment',
      state:  progress.feePayment
                ? 'done'
                : progress.documentUpload && !progress.formLocked
                ? 'active'
                : 'pending',
      dropdownTitle: null,
      pages: []
    },
    {
      number: 6,
      label:  'Lock form',
      state:  progress.formLocked
                ? 'done'
                : progress.feePayment
                ? 'active'
                : 'pending',
      dropdownTitle: null,
      pages: []
    },
  ]

  return (
    <div className="rounded-xl overflow-visible border border-gray-200 mb-4 shadow-sm">

      {/* Dark header */}
      <div className="bg-gray-900 px-5 py-3.5 rounded-t-xl flex items-center gap-2.5">
        <i className="fas fa-list text-gray-400 text-sm" />
        <span className="text-white font-semibold text-sm tracking-wide">Application Progress</span>
      </div>

      {/* Stepper body */}
      <div className="bg-white rounded-b-xl px-3 py-7">
        <div className="relative flex items-start justify-between w-full">

          {/* Connector line — from center of step 1 to center of step 6 */}
          <div
            className="absolute top-[22px] h-0.5 bg-gray-200 z-0"
            style={{ left: 'calc(100%/12)', right: 'calc(100%/12)' }}
          />

          {steps.map((step, idx) => (
            <StepItem key={idx} step={step} totalSteps={steps.length} />
          ))}

        </div>
      </div>
    </div>
  )
}

// ── Individual Step ───────────────────────────────────────────────────────────
function StepItem({ step }) {
  const [hovered, setHovered] = useState(false)
  const hasDropdown = step.pages.length > 0

  const circleClass = {
    done:    'bg-emerald-500 text-white border-0',
    active:  'bg-amber-400 text-white border-0',
    pending: 'bg-white text-gray-400 border-2 border-gray-200',
  }[step.state]

  const labelLines = Array.isArray(step.label) ? step.label : [step.label]

  return (
    <div className="flex flex-col items-center flex-1 relative z-10">

      {/* Circle + dropdown trigger */}
      <div
        className="relative"
        onMouseEnter={() => hasDropdown && setHovered(true)}
        onMouseLeave={() => hasDropdown && setHovered(false)}
      >
        {/* Circle */}
        <div className={`w-11 h-11 rounded-full flex items-center justify-center font-bold text-base ${circleClass}`}>
          {step.state === 'done'
            ? <i className="fas fa-check text-sm" />
            : step.number
          }
        </div>

        {/* Hover dropdown */}
        {hasDropdown && hovered && (
          <StepDropdown title={step.dropdownTitle} pages={step.pages} />
        )}
      </div>

      {/* Label */}
      <div className="mt-2 text-center text-[11px] text-gray-500 leading-snug max-w-[80px]">
        {labelLines.map((line, i) => (
          <span key={i} className="block">{line}</span>
        ))}
      </div>

    </div>
  )
}

// ── Dropdown popup ────────────────────────────────────────────────────────────
function StepDropdown({ title, pages }) {
  return (
    <div className="absolute top-[calc(100%+10px)] left-1/2 -translate-x-1/2
                    bg-white border border-gray-200 rounded-lg shadow-xl
                    min-w-[210px] z-50 py-2 whitespace-nowrap">

      {/* Arrow */}
      <div className="absolute -top-[7px] left-1/2 -translate-x-1/2
                      border-l-[7px] border-r-[7px] border-b-[7px]
                      border-l-transparent border-r-transparent border-b-gray-200" />
      <div className="absolute -top-[6px] left-1/2 -translate-x-1/2
                      border-l-[6px] border-r-[6px] border-b-[6px]
                      border-l-transparent border-r-transparent border-b-white" />

      {/* Header */}
      {title && (
        <div className="px-3.5 pb-2 pt-1 text-[10px] font-bold text-gray-400 uppercase tracking-wider border-b border-gray-100 mb-1">
          {title}
        </div>
      )}

      {/* Page rows */}
      {pages.map((page, i) => (
        <div key={i} className="flex items-center gap-2.5 px-3.5 py-1.5 hover:bg-gray-50 transition-colors">
          <DotIcon status={page.status} />
          <span className="text-[13px] text-gray-700">{page.label}</span>
        </div>
      ))}
    </div>
  )
}

// ── Status dot icon ───────────────────────────────────────────────────────────
function DotIcon({ status }) {
  if (status === 'green') {
    return (
      <span className="w-[18px] h-[18px] rounded-full bg-emerald-500 flex items-center justify-center flex-shrink-0">
        <i className="fas fa-check text-white" style={{ fontSize: '9px' }} />
      </span>
    )
  }
  if (status === 'orange') {
    return (
      <span className="w-[18px] h-[18px] rounded-full bg-orange-500 flex items-center justify-center flex-shrink-0 text-white text-xs">
        &#9679;
      </span>
    )
  }
  return (
    <span className="w-[18px] h-[18px] rounded-full border-2 border-gray-300 bg-white flex-shrink-0" />
  )
}

import { useEffect, useRef, useState } from 'react'
import './App.css'

const apiBase = import.meta.env.DEV
  ? 'http://localhost:5245/api'
  : '/api';

function App() {
  const [token, setToken] = useState<string | null>(null)
  const tokenRequestStarted = useRef(false)

  useEffect(() => {
    if (tokenRequestStarted.current) {
      return
    }

    tokenRequestStarted.current = true

    fetch(`${apiBase}/files/get-token`)
      .then(res => res.json())
      .then(data => {
        if (data.token) setToken(data.token)
      })
      .catch(err => console.error('Error fetching token:', err))
  }, [])

  const downloadUrl = token
    ? `${apiBase}/files/latest-oneroster?token=${token}`
    : '';

  return (
    <>
      <main className="page-shell">
        <div className="ambient ambient-one" aria-hidden="true" />
        <div className="ambient ambient-two" aria-hidden="true" />
        <div className="grain" aria-hidden="true" />

        <header className="topbar">
          <div className="brand-lockup">
            <span className="brand-mark" aria-hidden="true">D1</span>
            <div>
              <p className="eyebrow">Roster delivery</p>
              <p className="brand-name">
                Daily <span>OneRoster</span> File
              </p>
              <p className="brand-subtitle">Ready when the day starts</p>
            </div>
          </div>

          <div className="status-pill" aria-label="File generation status">
            <span className="status-dot" aria-hidden="true" />
            <span>K-12 OneRoster 1.1 generated daily at 12:00 AM</span>
          </div>
        </header>

        <section className="hero" aria-labelledby="hero-title">
          <div className="hero-copy">
            <p className="eyebrow eyebrow-accent">K-12 roster export</p>
            <h1 id="hero-title">A clean OneRoster 1.1 file, delivered without friction.</h1>
            <p className="hero-text">
              Download today's K-12 OneRoster 1.1 file now!
            </p>

            <div className="cta-row">
              <a className="download-button" href={downloadUrl}>
                <span>Download Today's File</span>
                <span className="download-arrow" aria-hidden="true">
                  <svg viewBox="0 0 24 24" role="presentation" focusable="false">
                    <path d="M12 4v10m0 0 4-4m-4 4-4-4" />
                    <path d="M5 19h14" />
                  </svg>
                </span>
              </a>
            </div>

            <div className="meta-row" aria-label="File details">
              <div className="meta-chip">
                <span className="meta-label">Format</span>
                <span className="meta-value">OneRoster 1.1 ZIP</span>
              </div>
              <div className="meta-chip">
                <span className="meta-label">Contents</span>
                <span className="meta-value">K-12 district export</span>
              </div>
              <div className="meta-chip">
                <span className="meta-label">Delivery</span>
                <span className="meta-value">Generated overnight</span>
              </div>
            </div>
          </div>

          <aside className="hero-panel" aria-label="File snapshot">
            <div className="panel-frame">
              <div className="panel-header">
                <span className="panel-kicker">Latest file</span>
                <span className="panel-badge">OneRoster 1.1</span>
              </div>

              <div className="file-card">
                <div className="file-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" role="presentation" focusable="false">
                    <path d="M7 3h6l4 4v14H7z" />
                    <path d="M13 3v5h5" />
                    <path d="M9 13h6" />
                    <path d="M9 16h6" />
                  </svg>
                </div>

                <div className="file-card-copy">
                  <p className="file-name">latest-oneroster.zip</p>
                  <p className="file-detail">Prepared for K-12 download</p>
                </div>
              </div>

              <dl className="stats-grid">
                <div>
                  <dt>Availability</dt>
                  <dd>24 / 7</dd>
                </div>
                <div>
                  <dt>Source</dt>
                  <dd>Automated pipeline</dd>
                </div>
                <div>
                  <dt>Audience</dt>
                  <dd>K-12 districts</dd>
                </div>
                <div>
                  <dt>Action</dt>
                  <dd>Single download</dd>
                </div>
              </dl>
            </div>
          </aside>
        </section>
      </main>
    </>
  )
}

export default App

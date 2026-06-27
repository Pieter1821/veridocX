import { useEffect, useState } from 'react';
import './App.css';

// Shape of GET /api/ping, matching the C# PingResponse record in VeridocX.Server.
interface PingResponse {
  service: string;
  status: string;
  time: string;
}

function App() {
  const [ping, setPing] = useState<PingResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  // On first render, call the API through Vite's /api proxy to prove the
  // React console can reach the C# backend. Replaced by real data in Phase 0.5.
  useEffect(() => {
    fetch('/api/ping')
      .then((res) => {
        if (!res.ok) throw new Error(`API returned ${res.status}`);
        return res.json() as Promise<PingResponse>;
      })
      .then(setPing)
      .catch((err: unknown) =>
        setError(err instanceof Error ? err.message : 'Failed to reach API')
      );
  }, []);

  return (
    <div className="app">
      <header className="app-header">
        <h1 className="app-title">VeridocX</h1>
        <p className="app-subtitle">
          Document intelligence for micro-lending. VeridocX produces signals. You decide.
        </p>
      </header>

      <main className="app-main">
        <section className="card" aria-labelledby="conn-heading">
          <h2 id="conn-heading" className="card-title">API connectivity</h2>

          {error && (
            <p className="status status-error" role="alert">
              Cannot reach the API: {error}
            </p>
          )}

          {!error && !ping && <p className="status">Checking connection…</p>}

          {ping && (
            <p className="status status-ok">
              Connected to <strong>{ping.service}</strong> ({ping.status}) at{' '}
              {new Date(ping.time).toLocaleTimeString()}
            </p>
          )}
        </section>
      </main>
    </div>
  );
}

export default App;

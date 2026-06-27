import { useCallback, useEffect, useState } from "react";
import { FileUpload } from "./components/ui/file-upload";
import { cn } from "./lib/utils";

interface PingResponse {
  service: string;
  status: string;
  time: string;
}

interface ValidationCheck {
  name: string;
  passed: boolean;
  detail: string;
}

interface SaIdValidationResult {
  input: string;
  isValid: boolean;
  checks: ValidationCheck[];
  dateOfBirth: string | null;
  gender: string | null;
  citizenship: string | null;
}

interface ExtractSaIdResponse {
  jobId: string;
  result: SaIdValidationResult;
  previouslySeen: number;
  extractedIdNumber: string;
  candidates: string[];
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

type UploadState = "idle" | "uploading" | "done" | "error";

const CHECK_LABELS: Record<string, string> = {
  format: "Format",
  date_of_birth: "Date of birth",
  gender: "Gender",
  citizenship: "Citizenship",
  checksum: "Checksum (Luhn)",
};

function App() {
  const [ping, setPing] = useState<PingResponse | null>(null);
  const [pingError, setPingError] = useState<string | null>(null);

  const [state, setState] = useState<UploadState>("idle");
  const [result, setResult] = useState<ExtractSaIdResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch("/api/ping")
      .then((res) => {
        if (!res.ok) throw new Error(`API returned ${res.status}`);
        return res.json() as Promise<PingResponse>;
      })
      .then(setPing)
      .catch((err: unknown) =>
        setPingError(err instanceof Error ? err.message : "Failed to reach API")
      );
  }, []);

  const upload = useCallback(async (file: File) => {
    setState("uploading");
    setResult(null);
    setError(null);

    try {
      const form = new FormData();
      form.append("file", file);

      const res = await fetch("/api/documents/sa-id/extract", {
        method: "POST",
        body: form,
      });

      if (!res.ok) {
        let message = `Request failed (${res.status})`;
        try {
          const problem = (await res.json()) as ProblemDetails;
          message = problem.detail ?? problem.title ?? message;
        } catch {
          /* no JSON body */
        }
        setError(message);
        setState("error");
        return;
      }

      setResult((await res.json()) as ExtractSaIdResponse);
      setState("done");
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Upload failed");
      setState("error");
    }
  }, []);

  const handleChange = useCallback(
    (files: File[]) => {
      const file = files[0];
      if (file) void upload(file);
    },
    [upload]
  );

  return (
    <div className="relative min-h-screen w-full bg-white text-neutral-900 dark:bg-black dark:text-neutral-100">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(#d4d4d4_1px,transparent_1px)] [background-size:18px_18px] [mask-image:radial-gradient(ellipse_at_center,black,transparent_80%)] dark:bg-[radial-gradient(#262626_1px,transparent_1px)]" />

      <div className="relative z-10 mx-auto flex min-h-screen w-full max-w-2xl flex-col px-4 py-12">
        <header className="text-center">
          <h1 className="bg-gradient-to-b from-neutral-900 to-neutral-600 bg-clip-text text-4xl font-bold tracking-tight text-transparent dark:from-white dark:to-neutral-400 sm:text-5xl">
            VeridocX
          </h1>
          <p className="mt-3 text-neutral-500 dark:text-neutral-400">
            Document intelligence for micro-lending. VeridocX produces signals. You
            decide.
          </p>
        </header>

        <main className="mt-10 flex flex-1 flex-col gap-6">
          <div className="w-full rounded-lg border border-dashed border-neutral-300 bg-white dark:border-neutral-800 dark:bg-black">
            <FileUpload onChange={handleChange} />
          </div>

          {state === "uploading" && (
            <p className="text-center text-sm text-neutral-500 dark:text-neutral-400">
              Reading document with OCR…
            </p>
          )}

          {state === "error" && error && (
            <p
              role="alert"
              className="rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-center text-sm text-red-400"
            >
              {error}
            </p>
          )}

          {state === "done" && result && <ResultCard data={result} />}
        </main>

        <footer className="mt-10 text-center text-xs text-neutral-500 dark:text-neutral-500">
          {pingError ? (
            <span className="text-red-400">API offline: {pingError}</span>
          ) : ping ? (
            <span>
              Connected to <strong>{ping.service}</strong> ({ping.status})
            </span>
          ) : (
            <span>Checking API…</span>
          )}
        </footer>
      </div>
    </div>
  );
}

function ResultCard({ data }: { data: ExtractSaIdResponse }) {
  const { result, extractedIdNumber, previouslySeen, candidates } = data;
  const others = candidates.filter((c) => c !== extractedIdNumber);

  return (
    <section className="rounded-xl border border-neutral-200 bg-white/70 p-6 backdrop-blur dark:border-neutral-800 dark:bg-neutral-950/60">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Result</h2>
        <span
          className={cn(
            "rounded-full px-3 py-1 text-xs font-bold uppercase tracking-wide",
            result.isValid
              ? "bg-emerald-500/15 text-emerald-400"
              : "bg-red-500/15 text-red-400"
          )}
        >
          {result.isValid ? "Valid" : "Invalid"}
        </span>
      </div>

      <p className="mt-3 font-mono tracking-wider">
        Extracted ID: <strong>{extractedIdNumber}</strong>
      </p>

      {result.isValid && (
        <dl className="mt-4 grid grid-cols-3 gap-3">
          <Fact label="Date of birth" value={result.dateOfBirth} />
          <Fact label="Gender" value={result.gender} />
          <Fact label="Citizenship" value={result.citizenship} />
        </dl>
      )}

      <ul className="mt-5 flex flex-col gap-2">
        {result.checks.map((c) => (
          <li key={c.name} className="flex items-start gap-3">
            <span
              className={cn(
                "mt-0.5 grid h-5 w-5 flex-none place-items-center rounded-full text-xs font-bold",
                c.passed
                  ? "bg-emerald-500/15 text-emerald-400"
                  : "bg-red-500/15 text-red-400"
              )}
            >
              {c.passed ? "✓" : "✕"}
            </span>
            <span className="flex flex-col">
              <span className="text-sm font-medium">
                {CHECK_LABELS[c.name] ?? c.name}
              </span>
              <span className="text-xs text-neutral-500 dark:text-neutral-400">
                {c.detail}
              </span>
            </span>
          </li>
        ))}
      </ul>

      <p
        className={cn(
          "mt-5 text-sm",
          previouslySeen > 0
            ? "text-amber-400"
            : "text-neutral-500 dark:text-neutral-400"
        )}
      >
        {previouslySeen > 0
          ? `This ID has been seen ${previouslySeen} time(s) before.`
          : "First time this ID has been submitted."}
      </p>

      {others.length > 0 && (
        <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-500">
          Other digit sequences detected: {others.join(", ")}
        </p>
      )}
    </section>
  );
}

function Fact({ label, value }: { label: string; value: string | null }) {
  return (
    <div>
      <dt className="text-[0.7rem] uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
        {label}
      </dt>
      <dd className="mt-1 font-semibold">{value ?? "—"}</dd>
    </div>
  );
}

export default App;

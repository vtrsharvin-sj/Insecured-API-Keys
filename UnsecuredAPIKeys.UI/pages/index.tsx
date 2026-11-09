import { useEffect, useState } from "react";
import Head from "next/head";
import { Button } from "@heroui/button";
import DefaultLayout from "@/layouts/default";
import { ApiKey, ApiKeyType, KeyStats } from "@/types";
import { fetchPaginated, fetchWithRateLimit } from "@/utils/api";

const PAGE_SIZE = 50;
type KeyPage = { items: ApiKey[]; totalCount: number; totalPages: number };

function asKeyPage(value: unknown): KeyPage {
  if (Array.isArray(value)) return { items: value as ApiKey[], totalCount: value.length, totalPages: 1 };
  const data = (value || {}) as Record<string, unknown>;
  const items = (data.items || data.keys || data.results || []) as ApiKey[];
  const totalCount = Number(data.totalCount ?? data.total ?? items.length);
  return { items, totalCount, totalPages: Number(data.totalPages ?? Math.max(1, Math.ceil(totalCount / PAGE_SIZE))) };
}

function formatDate(date?: string) { if (!date) return "—"; const parsed = new Date(date); return Number.isNaN(parsed.getTime()) ? date : parsed.toLocaleDateString(); }
function maskKey(value?: string) { return !value ? "—" : value.length <= 12 ? value : `${value.slice(0, 6)}…${value.slice(-4)}`; }

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-99]: { label: "Unverified", color: "bg-warning/10 text-warning" },
  [1]:   { label: "Valid ✔",    color: "bg-success/10 text-success" },
  [0]:   { label: "Invalid",    color: "bg-default/10 text-default-500" },
  [3]:   { label: "Removed",    color: "bg-danger/10 text-danger" },
  [4]:   { label: "Flagged",    color: "bg-danger/10 text-danger" },
  [5]:   { label: "Fixed",      color: "bg-default/10 text-default-500" },
  [6]:   { label: "Error",      color: "bg-danger/10 text-danger" },
  [7]:   { label: "No Credits", color: "bg-warning/10 text-warning" },
};
function statusBadge(status: number | undefined) {
  const s = STATUS_MAP[status ?? -99] ?? { label: "Unknown", color: "bg-default/10 text-default-500" };
  return <span className={`rounded-full px-2.5 py-1 text-xs font-medium ${s.color}`}>{s.label}</span>;
}

export default function IndexPage() {
  const [types, setTypes] = useState<ApiKeyType[]>([]);
  const [selectedType, setSelectedType] = useState("Random");
  const [page, setPage] = useState(1);
  const [keyPage, setKeyPage] = useState<KeyPage>({ items: [], totalCount: 0, totalPages: 1 });
  const [stats, setStats] = useState<KeyStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<number | null>(null);
  const [reportedIds, setReportedIds] = useState<number[]>([]);

  useEffect(() => {
    fetchWithRateLimit<ApiKeyType[]>("/API/GetKeyTypes", { requestId: "keyTypes" }).then((response) => { if (response.data) setTypes(response.data); });
    fetchWithRateLimit<KeyStats>("/API/GetKeyStatistics", { requestId: "keyStats" }).then((response) => { if (response.data) setStats(response.data); });
  }, []);

  useEffect(() => {
    let active = true;
    setLoading(true); setError(null);
    // selectedType is either "Random" (all) or the apiTypeId integer as a string
    const query = selectedType === "Random" ? "/API/GetAllValidKeys" : `/API/GetAllValidKeys?type=${encodeURIComponent(selectedType)}`;
    fetchPaginated<ApiKey>(query, page, PAGE_SIZE, { requestId: "validKeys" }).then((response) => {
      if (!active) return;
      if (response.data) setKeyPage(asKeyPage(response.data)); else setError(response.error || "Unable to load keys right now.");
      setLoading(false);
    });
    return () => { active = false; };
  }, [page, selectedType]);

  const total = stats?.totalNumberOfKeys ?? keyPage.totalCount;
  const valid = stats?.numberOfValidKeys ?? 0;
  const newToday = stats?.newKeysFoundToday ?? 0;
  const fixed = stats?.totalFixedKeys ?? 0;
  async function copyKey(key: ApiKey) { try { await navigator.clipboard.writeText(key.apiKey); setCopiedId(key.id); window.setTimeout(() => setCopiedId((id) => id === key.id ? null : id), 1500); } catch { setError("Clipboard access was unavailable."); } }
  async function reportKey(key: ApiKey) {
    const reference = key.references?.[0];
    const response = await fetchWithRateLimit("/API/TrackIssueSubmission", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ apiKeyId: key.id, apiType: key.apiType, repoUrl: reference?.repoURL ?? "" }),
      requestId: `report-${key.id}`,
    });
    if (response.error) setError(response.error);
    else setReportedIds((ids) => ids.includes(key.id) ? ids : [...ids, key.id]);
  }

  return <DefaultLayout><Head><title>Keys Finder | Unsecured API Keys</title><meta name="description" content="Browse verified API keys found in public code." /></Head><section className="py-10">
    <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between"><div><p className="text-sm font-semibold uppercase tracking-widest text-primary">Keys Finder</p><h1 className="text-4xl font-bold tracking-tight">Verified keys, in one place.</h1><p className="mt-2 text-default-500">Browse the latest keys found in public repositories and help keep the index clean.</p></div><select aria-label="Filter by key type" className="rounded-medium border border-default-200 bg-content1 px-4 py-2" value={selectedType} onChange={(event) => { setSelectedType(event.target.value); setPage(1); }}><option value="Random">All key types</option>{types.map((type) => <option key={type.apiType} value={String(type.apiTypeId)}>{type.apiType} ({type.keyCount})</option>)}</select></div>
    <div className="mt-8 grid grid-cols-2 gap-3 md:grid-cols-4">{[["Total indexed", total], ["Valid now", valid], ["New today", newToday], ["Fixed", fixed]].map(([label, value]) => <div className="rounded-xl border border-default-200 bg-content1 p-4" key={label as string}><p className="text-sm text-default-500">{label}</p><p className="mt-1 text-2xl font-semibold">{Number(value).toLocaleString()}</p></div>)}</div>
    <div className="mt-8 overflow-hidden rounded-xl border border-default-200 bg-content1"><div className="overflow-x-auto"><table className="w-full min-w-[900px] text-left text-sm"><thead className="border-b border-default-200 bg-default-50 text-default-500"><tr><th className="px-4 py-3">Type</th><th className="px-4 py-3">Key</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Found</th><th className="px-4 py-3">Repository</th><th className="px-4 py-3 text-right">Action</th></tr></thead><tbody className="divide-y divide-default-200">
      {loading && <tr><td className="px-4 py-10 text-center text-default-500" colSpan={6}>Loading keys…</td></tr>}{!loading && error && <tr><td className="px-4 py-10 text-center text-danger" colSpan={6}>{error}</td></tr>}{!loading && !error && keyPage.items.length === 0 && <tr><td className="px-4 py-10 text-center text-default-500" colSpan={6}>No keys found yet.</td></tr>}{!loading && !error && keyPage.items.map((key) => { const reference = key.references?.[0]; const reported = reportedIds.includes(key.id); return <tr key={key.id}><td className="px-4 py-3"><span className="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-medium text-primary">{key.apiType}</span></td><td className="px-4 py-3 font-mono">{maskKey(key.apiKey)} <Button size="sm" variant="light" onPress={() => copyKey(key)}>{copiedId === key.id ? "Copied" : "Copy"}</Button></td><td className="px-4 py-3">{statusBadge(key.status)}</td><td className="px-4 py-3 text-default-500">{formatDate(key.firstFoundUTC || key.lastFoundUTC)}</td><td className="px-4 py-3">{reference?.repoURL ? <a className="text-primary hover:underline" href={reference.repoURL} target="_blank" rel="noreferrer">{reference.repoName || reference.repoOwner || "View repo"}</a> : "—"}</td><td className="px-4 py-3 text-right"><Button size="sm" variant="flat" color={reported ? "success" : "default"} isDisabled={reported} onPress={() => reportKey(key)}>{reported ? "Reported" : "Report issue"}</Button></td></tr>; })}
    </tbody></table></div><div className="flex items-center justify-between border-t border-default-200 p-4"><span className="text-sm text-default-500">Page {page} of {Math.max(1, keyPage.totalPages)}</span><div className="flex gap-2"><Button size="sm" variant="flat" isDisabled={page <= 1 || loading} onPress={() => setPage((value) => value - 1)}>Previous</Button><Button size="sm" variant="flat" isDisabled={page >= keyPage.totalPages || loading} onPress={() => setPage((value) => value + 1)}>Next</Button></div></div></div>
  </section></DefaultLayout>;
}

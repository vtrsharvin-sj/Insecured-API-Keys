import Head from "next/head";
import { Link } from "@heroui/link";
import { Code } from "@heroui/code";
import { Snippet } from "@heroui/snippet";
import { Button } from "@heroui/button";
import NextLink from "next/link";
import { useState } from "react";

import { title, subtitle } from "@/components/primitives";
import DefaultLayout from "@/layouts/default";
import { fetchWithRateLimit } from "@/utils/api";

export default function AboutPage() {
  const [showTokenModal, setShowTokenModal] = useState(false);
  const [token, setToken]     = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [tokenMsg, setTokenMsg]     = useState<{ ok: boolean; text: string } | null>(null);

  async function submitToken() {
    if (!token.trim()) return;
    setSubmitting(true);
    setTokenMsg(null);
    const res = await fetchWithRateLimit<{ message?: string; error?: string }>("/API/SubmitToken", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Token: token.trim() }),
      requestId: "submitToken",
    });
    setSubmitting(false);
    if (res.data?.message) {
      setTokenMsg({ ok: true, text: res.data.message });
      setToken("");
    } else {
      setTokenMsg({ ok: false, text: res.data?.error ?? res.error ?? "Something went wrong." });
    }
  }
  return (
    <>
      <Head>
        <title>About - Insecured API Keys</title>
        <meta
          name="description"
          content="Learn about our mission to help secure the web by finding and reporting exposed API keys in public repositories. Making cybersecurity accessible to everyone."
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="keywords" content="about, mission, cybersecurity, API security, GitHub security, code vulnerability, security research" />
        <link rel="canonical" href="https://InsecuredAPIKeys.com/about/" />
        
        {/* Open Graph Meta Tags */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Insecured API Keys" />
        <meta property="og:title" content="About - Insecured API Keys" />
        <meta property="og:description" content="Learn about our mission to help secure the web by finding and reporting exposed API keys in public repositories." />
        <meta property="og:url" content="https://InsecuredAPIKeys.com/about/" />
        <meta property="og:image" content="https://InsecuredAPIKeys.com/og-image.png" />
        <meta property="og:image:width" content="1200" />
        <meta property="og:image:height" content="630" />
        <meta property="og:image:alt" content="About Insecured API Keys - Security Tool for Developers" />
        <meta property="og:locale" content="en_US" />
        
        {/* Twitter Card Meta Tags */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="About - Insecured API Keys" />
        <meta name="twitter:description" content="Learn about our mission to help secure the web by finding and reporting exposed API keys in public repositories." />
        <meta name="twitter:image" content="https://InsecuredAPIKeys.com/og-image.png" />
        <meta name="twitter:image:alt" content="About Insecured API Keys - Security Tool for Developers" />
        
        {/* Structured Data - About Page */}
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify({
              "@context": "https://schema.org",
              "@type": "AboutPage",
              "name": "About Insecured API Keys",
              "url": "https://InsecuredAPIKeys.com/about",
              "description": "Learn about our mission to help secure the web by finding and reporting exposed API keys in public repositories.",
              "mainEntity": {
                "@type": "Organization",
                "name": "Insecured API Keys",
                "description": "A security tool that helps developers find and report exposed API keys in public repositories.",
                "url": "https://InsecuredAPIKeys.com"
              }
            })
          }}
        />
        
        <link rel="icon" href="/favicon.ico" />
      </Head>

      <DefaultLayout>
        <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
          <div className="inline-block max-w-4xl text-center justify-center">
            <h1 className={title()}>About This Project</h1>
            <h2 className={subtitle({ class: "mt-4" })}>
              Making the web more secure, one exposed key at a time
            </h2>
          </div>

          <div className="max-w-4xl mt-8 space-y-8">
            <div className="text-center">
              <Snippet hideCopyButton hideSymbol variant="bordered">
                <span>
                  🎯 Our mission: Help developers avoid that awkward &quot;I just leaked our API keys&quot; conversation
                </span>
              </Snippet>
            </div>

            <div className="grid md:grid-cols-2 gap-8">
              <div className="space-y-6">
                <div>
                  <h3 className="text-2xl font-bold mb-4">What We Do 🔍</h3>
                  <p className="text-gray-600 leading-relaxed">
                    We continuously scan public GitHub repositories to find
                    exposed API keys that could be misused by malicious actors.
                    When we find these keys, we make them easily discoverable so
                    developers and security researchers can help notify the
                    repository owners.
                  </p>
                </div>

                <div>
                  <h3 className="text-2xl font-bold mb-4">Why It Matters 🚨</h3>
                  <p className="text-gray-600 leading-relaxed">
                    Exposed API keys can lead to:
                  </p>
                  <ul className="mt-3 text-gray-600 space-y-2">
                    <li>• Unauthorized access to paid services</li>
                    <li>• Data breaches and privacy violations</li>
                    <li>• Massive unexpected bills</li>
                    <li>• Reputational damage</li>
                    <li>• Security vulnerabilities</li>
                  </ul>
                </div>

                <div>
                  <h3 className="text-2xl font-bold mb-4">How You Can Help 🦸‍♂️</h3>
                  <p className="text-gray-600 leading-relaxed">
                    When you find an exposed key on our site, you can easily
                    submit a GitHub issue to notify the repository owner. This
                    gives them a chance to:
                  </p>
                  <ul className="mt-3 text-gray-600 space-y-2">
                    <li>• Revoke the compromised key</li>
                    <li>• Generate a new secure key</li>
                    <li>• Update their security practices</li>
                    <li>• Prevent potential abuse</li>
                  </ul>
                </div>
              </div>

              <div className="space-y-6">
                <div>
                  <h3 className="text-2xl font-bold mb-4">Supported Services 🔧</h3>
                  <p className="text-gray-600 leading-relaxed mb-4">
                    We currently detect API keys for these services:
                  </p>
                  <div className="grid grid-cols-2 gap-2 text-sm">
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#74AA9C] rounded-full"></span>
                      <span>OpenAI</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#5436DA] rounded-full"></span>
                      <span>Anthropic Claude</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#4285F4] rounded-full"></span>
                      <span>Google AI</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#F9B3AC] rounded-full"></span>
                      <span>Cohere</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#FFD21E] rounded-full"></span>
                      <span>Hugging Face</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#FF2BC2] rounded-full"></span>
                      <span>Stability AI</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#DE4815] rounded-full"></span>
                      <span>Mistral AI</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#000000] rounded-full"></span>
                      <span>Replicate</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#5B44F3] rounded-full"></span>
                      <span>Together AI</span>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="w-3 h-3 bg-[#3B82F6] rounded-full"></span>
                      <span>OpenRouter</span>
                    </div>
                  </div>
                </div>

                <div>
                  <h3 className="text-2xl font-bold mb-4">Best Practices 📋</h3>
                  <p className="text-gray-600 leading-relaxed mb-4">
                    To avoid exposing your API keys:
                  </p>
                  <ul className="text-gray-600 space-y-2">
                    <li>• Use environment variables (</li>
                    <li className="ml-4">
                      <Code className="text-xs">process.env.API_KEY</Code>)
                    </li>
                    <li>• Add <Code className="text-xs">.env</Code> to your{" "}
                      <Code className="text-xs">.gitignore</Code>
                    </li>
                    <li>• Use secret management services</li>
                    <li>• Regularly rotate your API keys</li>
                    <li>• Never commit secrets to version control</li>
                    <li>• Scan your repos before pushing</li>
                  </ul>
                </div>

                <div>
                  <h3 className="text-2xl font-bold mb-4">Disclaimer ⚖️</h3>
                  <p className="text-gray-600 leading-relaxed text-sm">
                    This project is for educational and security research
                    purposes only. We do not use, store, or misuse any of the
                    discovered API keys. Our goal is to help repository owners
                    secure their keys before they can be exploited maliciously.
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-12 text-center">
              <h3 className="text-2xl font-bold mb-4">Ready to Help Secure the Web? 🌐</h3>
              <p className="text-gray-600 mb-6">
                Join our community of security-minded developers and help make
                the internet a safer place for everyone.
              </p>
              <div className="flex justify-center space-x-4">
                <NextLink
                  href="/"
                  className="inline-flex items-center px-6 py-3 bg-primary text-white rounded-full hover:bg-primary-600 transition-colors"
                >
                  Start Finding Keys 🔍
                </NextLink>
                <NextLink
                  href="/leaderboard"
                  className="inline-flex items-center px-6 py-3 border border-primary text-primary rounded-full hover:bg-primary hover:text-white transition-colors"
                >
                  View Leaderboard 🏆
                </NextLink>
              </div>
            </div>
          </div>

          {/* ── Contribute a GitHub Token ── */}
          <div className="mt-12 w-full max-w-4xl">
            <div className="rounded-xl border border-default-200 bg-content1 p-8 text-center">
              <h3 className="text-2xl font-bold mb-2">Boost the Scraper 🚀</h3>
              <p className="text-default-500 mb-6 text-sm">
                The scraper uses GitHub tokens to search for exposed keys. More tokens = more rate limit headroom = faster discovery.
                If you have a spare GitHub PAT with <code className="bg-default-100 px-1 rounded">public_repo</code> scope, contribute it here.
              </p>
              <Button color="primary" variant="flat" onPress={() => { setShowTokenModal(true); setTokenMsg(null); }}>
                Contribute a GitHub Token
              </Button>
            </div>
          </div>

          {/* ── Token submission modal ── */}
          {showTokenModal && (
            <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4" onClick={() => setShowTokenModal(false)}>
              <div className="w-full max-w-md rounded-xl bg-content1 border border-default-200 p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
                <h2 className="text-xl font-bold mb-1">Contribute a GitHub Token</h2>
                <p className="text-default-500 text-sm mb-4">
                  Create a token at{" "}
                  <a href="https://github.com/settings/tokens/new?scopes=public_repo&description=InsecuredAPIKeys+Scraper" target="_blank" rel="noreferrer" className="text-primary hover:underline">
                    github.com/settings/tokens
                  </a>{" "}
                  with <code className="bg-default-100 px-1 rounded">public_repo</code> scope, then paste it below.
                  Your token is validated and stored securely — it's only used for GitHub code search.
                </p>
                <input
                  type="password"
                  placeholder="github_pat_..."
                  value={token}
                  onChange={(e) => setToken(e.target.value)}
                  className="w-full rounded-lg border border-default-200 bg-default-50 px-3 py-2 text-sm font-mono mb-3 focus:outline-none focus:border-primary"
                  onKeyDown={(e) => e.key === "Enter" && !submitting && submitToken()}
                  autoComplete="off"
                />
                {tokenMsg && (
                  <p className={`text-sm mb-3 ${tokenMsg.ok ? "text-success" : "text-danger"}`}>
                    {tokenMsg.text}
                  </p>
                )}
                <div className="flex gap-2 justify-end">
                  <Button size="sm" variant="flat" onPress={() => setShowTokenModal(false)}>Cancel</Button>
                  <Button size="sm" color="primary" isLoading={submitting} isDisabled={!token.trim()} onPress={submitToken}>
                    Submit Token
                  </Button>
                </div>
              </div>
            </div>
          )}
        </section>
      </DefaultLayout>
    </>
  );
}

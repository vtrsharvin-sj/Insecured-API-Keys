import Head from "next/head";
import { Button } from "@heroui/button";
import { Card, CardBody, CardHeader } from "@heroui/card";
import { CheckIcon } from "@heroicons/react/24/outline";
import NextLink from "next/link";

import { title, subtitle } from "@/components/primitives";
import DefaultLayout from "@/layouts/default";

export default function PricingPage() {
  return (
    <>
      <Head>
        <title>Pricing - Insecured API Keys</title>
        <meta
          name="description"
          content="Our pricing is simple: completely free! Help secure the web without any cost. No hidden fees, no premium tiers - just free API key security tools."
        />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="keywords" content="pricing, free security tools, API key security, cost, GitHub security, free cybersecurity, developer tools" />
        <link rel="canonical" href="https://InsecuredAPIKeys.com/pricing/" />

        {/* Open Graph Meta Tags */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content="Insecured API Keys" />
        <meta property="og:title" content="Pricing - Insecured API Keys" />
        <meta property="og:description" content="Our pricing is simple: completely free! Help secure the web without any cost. No hidden fees, no premium tiers." />
        <meta property="og:url" content="https://InsecuredAPIKeys.com/pricing/" />
        <meta property="og:image" content="https://InsecuredAPIKeys.com/og-image.png" />
        <meta property="og:image:width" content="1200" />
        <meta property="og:image:height" content="630" />
        <meta property="og:image:alt" content="Pricing - Free Security Tools at Insecured API Keys" />
        <meta property="og:locale" content="en_US" />

        {/* Twitter Card Meta Tags */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="Pricing - Insecured API Keys" />
        <meta name="twitter:description" content="Our pricing is simple: completely free! Help secure the web without any cost." />
        <meta name="twitter:image" content="https://InsecuredAPIKeys.com/og-image.png" />
        <meta name="twitter:image:alt" content="Pricing - Free Security Tools at Insecured API Keys" />

        {/* Structured Data - Pricing */}
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify({
              "@context": "https://schema.org",
              "@type": "WebPage",
              "name": "Pricing - Insecured API Keys",
              "url": "https://InsecuredAPIKeys.com/pricing",
              "description": "Our pricing is simple: completely free! Help secure the web without any cost.",
              "mainEntity": {
                "@type": "Product",
                "name": "Insecured API Keys Security Tool",
                "description": "Free tool to find and report exposed API keys in public repositories",
                "offers": {
                  "@type": "Offer",
                  "price": "0",
                  "priceCurrency": "USD",
                  "availability": "https://schema.org/InStock",
                  "priceValidUntil": "2030-12-31"
                }
              }
            })
          }}
        />

        <link rel="icon" href="/favicon.ico" />
      </Head>

      <DefaultLayout>
        <section className="flex flex-col items-center justify-center gap-4 py-8 md:py-10">
          <div className="inline-block max-w-4xl text-center justify-center">
            <h1 className={title()}>Pricing</h1>
            <h2 className={subtitle({ class: "mt-4" })}>
              Simple, transparent, and completely free
              <br />
              <span className="italic">
                Because security shouldn&apos;t cost a fortune 💸
              </span>
            </h2>
          </div>

          <div className="max-w-5xl mt-8">
            <div className="grid md:grid-cols-2 gap-6">
              {/* Free Tier */}
              <Card className="relative border-2 border-primary overflow-visible mt-8">
                <div className="absolute -top-3 left-1/2 transform -translate-x-1/2 z-10">
                  <div className="bg-primary text-white px-4 py-2 rounded-full text-xs font-semibold whitespace-nowrap">
                    Most Popular for Poor People
                  </div>
                </div>
                <CardHeader className="text-center pt-12">
                  <h3 className="text-xl font-bold">Free Forever</h3>
                  <div className="mt-4">
                    <span className="text-4xl font-bold">$0</span>
                    <span className="text-gray-500 text-sm">/forever</span>
                  </div>
                  <p className="text-gray-600 mt-2 text-sm">
                    Perfect for people who don&apos;t want to see this site live forever
                  </p>
                </CardHeader>
                <CardBody className="pt-0">
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>Unlimited API key viewing</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>GitHub issue submission</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>Leaderboard participation</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>All supported API types</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-green-500 mr-2" />
                      <span>Real-time statistics</span>
                    </li>
                  </ul>
                  <div className="space-y-3 mt-4">
                    <NextLink href="/">
                      <Button
                        color="primary"
                        variant="shadow"
                        className="w-full"
                        size="md"
                      >
                        Get Started Now
                      </Button>
                    </NextLink>
                  </div>
                </CardBody>
              </Card>

              {/* Premium Tier (Joke) */}
              <Card className="relative border-2 border-pink-500 overflow-visible mt-8">
                <div className="absolute -top-3 left-1/2 transform -translate-x-1/2 z-10">
                  <div className="bg-gradient-to-r from-pink-500 to-rose-500 text-white px-4 py-2 rounded-full text-xs font-semibold whitespace-nowrap">
                    💸 For Premium Users
                  </div>
                </div>
                <CardHeader className="text-center pt-12">
                  <h3 className="text-xl font-bold text-gray-400">Premium</h3>
                  <div className="mt-4">
                    <span className="text-4xl font-bold text-gray-400">$∞</span>
                    <span className="text-gray-500 text-sm">/forever</span>
                  </div>
                  <p className="text-gray-400 mt-2 text-sm">
                    For those who want to support the project
                  </p>
                </CardHeader>
                <CardBody className="pt-0">
                  <ul className="space-y-2 text-sm">
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Same as free tier</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">But costs money</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Bragging rights</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Golden badge*</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Our eternal gratitude</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">Optionally included in our supporters page</span>
                    </li>
                    <li className="flex items-center">
                      <CheckIcon className="h-4 w-4 text-gray-300 mr-2" />
                      <span className="text-gray-400">*Badge not included</span>
                    </li>
                  </ul>
                  <div className="space-y-3 mt-4">
                    <a href="https://buymeacoffee.com/sharvinsj" target="_blank" rel="noreferrer" className="block w-full">
                      <Button
                        color="default"
                        variant="flat"
                        className="w-full"
                        size="md"
                      >
                        ☕ Buy me a coffee
                      </Button>
                    </a>
                  </div>
                </CardBody>
              </Card>
            </div>

            <div className="mt-12 text-center">
              <h3 className="text-2xl font-bold mb-4">Frequently Asked Questions 🤔</h3>
              <div className="grid md:grid-cols-2 gap-8 mt-8 text-left">
                <div>
                  <h4 className="font-bold mb-2">Is this really free?</h4>
                  <p className="text-gray-600 text-sm">
                    Yes! This is a passion project to help improve web security.
                    No hidden costs, no premium tiers, no enterprise nonsense.
                  </p>
                </div>
                <div>
                  <h4 className="font-bold mb-2">What&apos;s the catch?</h4>
                  <p className="text-gray-600 text-sm">
                    There isn&apos;t one. Just help make the web more secure by
                    reporting exposed keys when you find them.
                  </p>
                </div>
                <div>
                  <h4 className="font-bold mb-2">Will you add paid features?</h4>
                  <p className="text-gray-600 text-sm">
                    Highly unlikely. The core mission of helping secure exposed
                    API keys should remain free and accessible to everyone.
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-12 text-center">
              <div className="bg-gray-50 rounded-lg p-8">
                <h3 className="text-xl font-bold mb-4">The Real Cost 💭</h3>
                <p className="text-gray-600 mb-4">
                  While our service is free, exposed API keys can cost developers
                  and companies thousands of dollars in unauthorized usage,
                  security breaches, and remediation efforts.
                </p>
                <p className="text-gray-600">
                  Help us prevent these costs by responsibly reporting exposed
                  keys. Together, we can make the web a more secure place! 🛡️
                </p>
              </div>
            </div>
          </div>
        </section>
      </DefaultLayout>
    </>
  );
}

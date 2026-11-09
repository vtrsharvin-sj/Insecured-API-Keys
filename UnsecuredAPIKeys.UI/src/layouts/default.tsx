import { Link } from "@heroui/link";
import React, { useState } from "react";
import { Button } from "@heroui/button";

import { Navbar } from "@/components/navbar";
import Disclaimer from "@/components/Disclaimer";

export default function DefaultLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [showDrawer, setShowDrawer] = useState(false);

  return (
    <div className="relative flex flex-col min-h-screen dark:bg-gradient-to-br dark:from-black dark:via-zinc-950 dark:to-slate-950">
      <Navbar />
      <main className="container mx-auto max-w-7xl px-6 flex-grow pt-16">
        {children}
      </main>
      <footer className="w-full flex items-center justify-center py-3 gap-4 flex-wrap text-sm">
        <Link
          isExternal
          className="flex items-center gap-1 text-current"
          href="https://www.DiploWords.com"
          title="DiploWords.com homepage"
        >
          <span className="text-default-600">Powered by</span>
          <p className="text-primary">DiploWords [Ai]</p>
        </Link>

        {/* GitHub Project Attribution — required by license */}
        <Link
          isExternal
          className="text-default-600 hover:text-primary"
          href="https://github.com/TSCarterJr/UnsecuredAPIKeys-OpenSource"
          title="View source code on GitHub"
        >
          Based on UnsecuredAPIKeys Open Source
        </Link>

        {/* Legal Disclaimer */}
        <Button
          className="text-default-600 hover:text-primary"
          size="sm"
          variant="light"
          onPress={() => setShowDrawer(true)}
        >
          Legal Disclaimer
        </Button>

        {showDrawer && (
          <Disclaimer isOpen={showDrawer} onClose={() => setShowDrawer(false)} />
        )}
      </footer>
    </div>
  );
}

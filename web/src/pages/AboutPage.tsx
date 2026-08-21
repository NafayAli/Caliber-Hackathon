import { Mail } from 'lucide-react'
import { InsetGroupedList, Row } from '../components/ios/InsetGroupedList'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'

export function AboutPage() {
  return (
    <div className="mx-auto max-w-2xl">
      <LargeTitleHeader title="About" subtitle="Calibration & Performance" />

      <div className="mb-8 flex flex-col items-center rounded-2xl bg-grouped px-6 py-10 text-center shadow-sm ring-1 ring-separator/40">
        <img
          src="/caliber-logo.svg"
          alt="Caliber"
          className="mb-6 h-20 w-20 object-contain"
        />
        <h2 className="text-2xl font-bold tracking-tight text-label">Caliber</h2>
        <p className="mt-2 text-base font-medium text-accent">Calibration & Performance</p>
        <p className="mt-4 max-w-md text-sm leading-relaxed text-secondary-label">
          Caliber helps equipment dealership service teams track certifications, training,
          and skills — so every technician is ready before the wrench turns. Built for
          managers who need clarity and technicians who need a clear path to compliance.
        </p>
      </div>

      <section className="mb-6">
        <h3 className="mb-2 px-4 text-xs font-semibold uppercase tracking-wide text-secondary-label">
          Developer
        </h3>
        <InsetGroupedList>
          <Row
            label="Nafay Ali"
            description="Database Developer"
            accessory={
              <a
                href="mailto:nafay.ali@constellationdealer.com"
                className="inline-flex items-center gap-1 text-sm font-medium text-accent hover:underline"
              >
                <Mail className="h-4 w-4" />
                Email
              </a>
            }
          />
          <div className="border-t border-separator/60 px-4 py-3">
            <a
              href="mailto:nafay.ali@constellationdealer.com"
              className="text-sm text-secondary-label hover:text-accent"
            >
              nafay.ali@constellationdealer.com
            </a>
          </div>
        </InsetGroupedList>
      </section>

      <section>
        <h3 className="mb-2 px-4 text-xs font-semibold uppercase tracking-wide text-secondary-label">
          Version
        </h3>
        <InsetGroupedList>
          <Row label="Hackathon build" description="August 2026 · Constellation Dealer" />
        </InsetGroupedList>
      </section>
    </div>
  )
}

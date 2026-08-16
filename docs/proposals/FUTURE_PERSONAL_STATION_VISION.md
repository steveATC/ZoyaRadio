# Zoya Radio future personal-station vision

## Status and authority

This is **future product vision and unapproved product-design material**. It is not
current architecture, an approved roadmap, a milestone contract, or implementation
scope. Exact names, controls, examples, schemas, models, providers, schedules, and
behaviors remain illustrative unless Steve later promotes them through the project's
decision and roadmap process.

The accepted Milestone 8 runtime baseline and the Milestone 9 closure remain governed
by the canonical project documents. This vision neither accepts M9B as runtime behavior
nor authorizes post-M9 implementation.

The companion [post-M8 proposal intake](POST_M8_INTAKE.md) remains the detailed
architecture/proposal inventory. This document provides a coherent product-level view
of what those capabilities might eventually support.

## 1. A personal radio-station engine

A mature Zoya Radio need not give every listener the same host named Zoya. It may become
a local-first personal station engine from which very different stations can be built.
Zoya can remain Steve's original station, host, and default demonstration of the
concept without becoming the only possible identity.

Two installations using the same engine could differ in language, listener relationship,
host personality, editorial theme, station sound, enabled formats, continuity, and
production style. The common product is the safe music-first radio engine, not one fixed
character or genre.

Music remains primary. Silence remains valid. A more capable station is not required to
use every available format, and prepared material never earns a right to airtime.

## 2. Understandable station configuration

Future listeners should configure recognizable editorial concepts rather than edit raw
prompts or operate production clocks. A canonical station/listener profile may eventually
represent concepts such as:

- station name, language, locale, theme/editorial identity, Station Sound, and desired
  host presence;
- listener name, preferred form of address, interests, supplied context, and topics to
  avoid;
- host name, voice identity, personality, interests, relationship to the listener,
  teasing/profanity/energy preferences, and optional flavor or backstory;
- enabled or disabled radio formats, including reactions, longer bits, fictional calls,
  fictional commercials, imaging, and recurring station-world material;
- privacy, network, factual-enrichment, provider, memory, and Sound Pack policies.

These are possible dimensions, not a final schema or UI. Ordinary controls should express
broad intent—such as low/balanced/high host presence or never/rare/sometimes for a format.
Independent clocks, cooldown seconds, probabilities, and production percentages should
normally remain system concerns.

Structured configuration may influence creativity, but it must not expose system
authority. A raw prompt editor must not be able to override Safe Playback, targeting,
scheduling restraint, factual grounding, trusted-source rules, privacy, provenance,
cancellation, deterministic state, or silence decisions.

## 3. Station Setup and On Air are different experiences

The future product may distinguish two broad experiences:

1. **Station Setup** creates or edits the station, auditions important identity choices,
   supplies optional starting premises, and prepares an initial bounded asset inventory.
2. **On Air** is the normal listening experience. It emphasizes station and host identity,
   Now Playing, understandable radio activity, Start/Stop/Silent Mode, useful session
   statistics, and compact continuity or Station Lore.

Model names, logs, technical readiness, and maintenance diagnostics may remain available
without dominating the mature On Air experience. This is future UX direction only; it
does not redesign the current WinForms application.

## 4. Configuration, seed material, and earned history

Future continuity should distinguish three classes of station context:

- **Configuration** is explicitly established by the listener: names, personality,
  interests, editorial character, relationships, and enabled formats.
- **Seed material** is an optional creative premise the station may develop, ignore,
  defer, or retire. A suggested caller, advertiser, rivalry, or running-joke premise is
  not evidence that the event already happened on air.
- **Earned history** records what was deterministically observed or actually established
  during the station's lifetime: an aired call, recurring advertiser, repeated artist,
  developed joke, station event, or relationship change.

This separation allows a new station to have a head start without fabricating its lived
history. The listener supplies premises, not scripts, and supplied material creates no
scheduling obligation.

Optional starting material may use small natural-language inputs for people/caller ideas,
fictional products, host habits, listener context, or general station-world premises.
The system may structure those inputs internally later.

## 5. First broadcast and bounded premiere material

Setup may offer an optional one-time **Introduce the station** choice. Selecting it creates
an introductory opportunity; it does not force speech when Start is pressed. The
introduction should wait for a safe, natural early boundary, may be displaced or expire,
and should not replay on every application launch.

A brief introduction might establish the station name, host name, tone, and one piece of
flavor without dumping configuration or lore. Before the first broadcast, setup may also
prepare a small bounded inventory of IDs, sonic motifs, optional caller/commercial
candidates, or short host material.

Prepared does not mean scheduled. Scheduled does not mean entitled to play. Complex
material should be ready before its first audible beat, and a new station should not
produce a compulsory first-session content dump.

## 6. Host personality, editorial identity, and Station Sound

Several identities should remain distinct:

- **Host personality** describes how the presenter behaves.
- **Host interests** describe subjects the presenter finds creatively interesting.
- **Station theme/editorial identity** describes the station's broader world and center
  of attention.
- **Station Sound** is a possible user-facing description of sonic/production character.
- **Sonic Identity** is a possible internal design term.
- **Sound Pack** is a possible term for the accepted prepared asset family.

These names are not final UI decisions. The separation matters because an energetic host
can belong to a restrained station sound, and two hosts with similar personalities can
live inside radically different editorial worlds.

## 7. Sonic identity is normally prepared before broadcast

Station sonic identity should normally be authored during setup, customization, or an
explicit later preparation workflow—not generated from scratch while ordinary music is
playing. A plausible progressive workflow is:

1. prepare a very small number of candidate master identities or station IDs;
2. let the listener audition meaningful alternatives, such as choosing A or B;
3. treat the selected direction as the station's sonic DNA;
4. derive a coherent family of related assets;
5. curate and store accepted results locally;
6. let runtime treat them as ordinary reusable typed assets.

The listener should judge which direction feels like the station, not configure compressor
ratios, EQ curves, reverb values, pitch contours, or loudness targets.

A future pack might contain a motif, full and micro IDs, stings, bumpers, sweeps,
transitions, beds, announcer profiles, branded effects, or intensity/late-night variants.
That list is illustrative rather than a frozen schema.

A default Zoya experience may ship with or reference one curated canonical pack. Custom
generation becomes more relevant when a listener creates a different station name, host,
language, cultural context, or sonic character. Not every installation needs unique
generated branding.

An explicit future **Refresh Station Imaging** workflow may add or replace variants while
preserving recognizable motifs and production continuity instead of regenerating an
unrelated identity.

## 8. Imaging speech and runtime host speech have different constraints

Pre-produced station imaging is reusable, stable, and relatively insensitive to creation
latency. It may use a heavier or more expressive renderer, generate multiple takes, retain
only the best performance, and receive additional production treatment before acceptance.
Once accepted, the result is a local audio asset; fixed IDs need not be synthesized again
every time they air.

Normal host commentary is dynamic and context-dependent. It is normally rendered near
runtime and must respect preparation time, resources, cancellation, safe playback, and
local-first constraints. It may use a lighter or different voice renderer.

The two paths do not need the same model or provider.

## 9. Script, voice identity, performance, and production are separate

Future voice and audio design should preserve four separate concerns:

- what is said;
- who is speaking and the recognizable voice identity;
- how the line is performed;
- how the performance is mixed and produced.

One recognizable host voice might perform conversational, warm, amused, deadpan,
irritated, celebratory, intimate, conspiratorial, ranting, or announcer-style material.
A voice model/file should not be equated with one emotional delivery.

Provider-neutral Voice Direction may express semantic intent such as emotion, energy,
pace, delivery style, emphasis, attitude, and optional paralinguistic behavior. Exact fields
remain open. A renderer adapter can translate supported intent into emotion/style controls,
speed, pitch, emphasis, shouting, whispering, laughter, or a smaller supported subset.
Graceful degradation is preferable to pretending unsupported acting occurred.

Excessive punctuation or deliberately mangled script text should not become the canonical
representation of emotion. Backend-specific text adjustment may exist as a renderer
technique while remaining separate from the authoritative spoken script and Voice Direction.

Performance is also distinct from production treatment. Compression, EQ, saturation,
doubling, delay/reverb, telephone/radio filtering, impacts, music-bed placement, ducking,
and level matching belong to audio production rather than acting instructions.

## 10. Shared production tools and station-specific identity

Not every asset must be unique to a station. Appropriately licensed utility effects—such
as telephone rings, paper, doors, room tone, crowds, vinyl/tape noise, or generic impacts—
can be shared production tools. Station-specific identity is more likely to live in its
motif, selected imaging, station-name performances, distinctive stings, announcer
treatment, and recurring branded cues.

Accepted creative assets may eventually need provenance describing origin, provider/model
and version, creation date, local/cloud/user/human source, license or usage status where
relevant, station/pack association, and derivation from a master asset. No specific schema
or current provider claim is approved here.

Runtime should mainly care that an asset is valid, available, correctly typed, eligible,
and safe. Whether it was generated locally, produced manually, assembled from stock FX,
or rendered through an optional service belongs to preparation and asset management.

## 11. Station Lore makes durable continuity understandable

A mature station may expose a compact, inspectable view of accumulated continuity:
recurring callers and advertisers, running jokes, characters, host interests or grudges,
important station events, unresolved threads, relationships, recurring phrases, and other
station-world developments.

Possible user-facing terms include **Station Lore** or **Station World**; neither is final.
The purpose is not gamification for its own sake. It is to help the listener understand
what the station remembers and provide ways to correct, retire, or preserve useful
continuity.

Selective retention remains essential. Established jokes should often compress, one-off
material should normally fade, and continuity should never crowd out music.

## 12. Trusted knowledge and creative freedom

A host interest is not factual authority. Configuring a host as obsessed with a game,
series, religion, sport, or historical subject can increase creative relevance and trusted
retrieval priority; it does not make everything remembered by a language model true.

The product should distinguish:

- claims about external reality or fictional canon, which require appropriate trusted
  evidence; and
- clearly fictional host/station play around a subject, which may be absurd so long as it
  is not presented as a factual claim about that subject.

Authoritative material may require a separate retrieved source: exact quotations, Bible
passages or doctrine, scientific facts, sports scores, schedules, financial/current-market
information, media canon, and similar verifiable content. The host may react creatively
around supplied trusted content without becoming its factual source.

True and well-sourced information may still be ineligible under a listener's spoiler
policy. Future eligibility may need to distinguish unsupported, uncertain, permitted, and
true-but-not-permitted-for-this-listener knowledge. Exact progress/schema mechanics remain
unresolved.

## 13. Editorial and role boundaries

Host personality is not the complete station editorial policy. A station-wide layer may
eventually constrain profanity, sexual or graphic material, religious treatment, listener
teasing, family-safe operation, and topic-specific respect across hosts, callers,
commercials, imaging, stories, and templates.

Topic-specific boundaries can permit an irreverent host to treat grief, trauma, religion,
family, or cultural subjects differently without turning one example into a universal
morality system.

Fictional relationship framing also does not grant real authority. A station character
portrayed as clergy, doctor, lawyer, financial adviser, teacher, or historian must not
automatically claim the corresponding real-world credentials, powers, or authoritative
rulings.

## 14. Formats are capabilities, not obligations

The engine may know how to produce calls, commercials, rants, imaging, or multi-voice
scenes while a particular station disables them completely. Inventory availability and
editorial cadence remain separate. A station with fictional commercials set to **Never**
has no fictional commercials; the scheduler should not compensate because the subsystem
exists.

Likewise, calls may be disabled, imaging may be minimal, host presence may be low, and
long stretches of music may remain untouched.

## 15. Illustrative stress-test stations

The following examples test whether one engine can support genuinely different stations.
They are **non-normative examples**, not required presets, built-in characters, or approved
content.

### TryHard Radio

- Listener: Mortorro
- Host: Striker
- Language/locale: PT-BR / Brazil
- Theme: competitive gaming
- Possible material: gaming rivals, ridiculous FPS grievances, and fictional gaming
  products

This tests a high-energy comedic station and recurring competitive mythology.

### Coração FM

- Listener: Suzy
- Host: Roberto Martins
- Language/locale: PT-BR / Brazil
- Theme: romance and relationships
- Premise: Roberto considers himself a romantic expert while repeatedly proving otherwise
- Possible seeds: callers affected by bad advice, romantic complications, or a fictional
  pickup-line application

This tests how optional premises may grow into earned callers, relationships, advertiser
interactions, and a running “Método Martins” mythology without pretending those events
already occurred.

### Capela FM

- Listener: Matias
- Host: Padre Miguel
- Language/locale: PT-BR / Brazil
- Theme: Catholic/religious companion radio
- Tone: energetic and irreverent about ordinary life while respectful toward sacred
  subjects
- Fictional commercials: disabled
- Possible material: sourced Bible passages or religious/history context, listener
  questions, parish-life humor, and restrained running jokes

This tests trusted authoritative content, respect boundaries, fictional-role limits, and a
station that intentionally disables an entire format.

## 16. Provider neutrality, localization, and evaluation

No current voice, language model, audio generator, or cloud service becomes an approved
dependency because it appeared in brainstorming. Replaceability is the durable idea.
Future choices should be evaluated for expressive quality, identity consistency, latency,
resource use, language/localization quality, license/distribution suitability,
local/offline operation, privacy, cost, and reliability.

Localization is not translation. Language, locale, host identity, listener identity,
Station Sound, voice, and editorial policy remain separate. Locale may inform production
without forcing stereotypical instruments, accents, genres, characters, or cultural
references.

## 17. Relationship to future planning

This vision explains possibilities; it does not choose a build order. The intake's
architecture-gap cards describe supporting responsibilities such as station/session
configuration, capabilities, trust, persistence, assets, editorial policy, decision
tracing, resource governance, and feedback.

A later project-planning decision must decide which ideas to accept, reject, research,
experiment with, park, or promote into bounded milestones. Until that happens, no feature
here is implementation scope.

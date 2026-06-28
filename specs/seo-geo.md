# SEO / GEO — master-dokument

Single source of truth för SEO/GEO-strategi, status och backlog för *Mer eller Mindre*
(kvissig.se). Allt SEO/GEO-arbete ägs här; `tasks.md` pekar bara hit.

GEO = Generative Engine Optimization (synlighet/citerbarhet i AI-svar: ChatGPT, Claude,
Perplexity, Google AI Overviews).

---

## 1. Strategi & principer

**Kilen (vad vi säljer in):** tvåstegs-twisten (gissa *riktning* mer/mindre → sedan
*differens*) + online-tillsammans (spela på distans med familj/vänner). Det är differen­
tieringen mot fysiska kortlekar och mot enstegs-gissningsspel.

**HÅRD REGEL — ordet "gratis" får ALDRIG förekomma** någonstans (UI, copy, schema, docs,
detta dokument). Spelet kan monetiseras i framtiden; kilen är twisten + online-tillsammans,
ALDRIG pris. Lint: `grep -i gratis src` och `grep -i gratis specs/seo-geo.md` → 0 träffar.

**White-hat 0-100-jämförelse:** vi jämför ärligt mot PlayMIGs *0-100* för long-tail-intent.
Ingen varumärkesimitation, ingen antydd koppling till/partnerskap med PlayMIG. Faktabaserad
jämförelsetabell, inte nedsvärtning.

---

## 2. Keyword / intent-mål

Vi kan INTE ranka på rena **"0-100"** — PlayMIG äger termen och den är brusig
(bilacceleration 0-100 km/h, mattespel, etc.). Vi tar long-tail/jämförelse-intent där
köparen redan letar efter ett *spel som vårt*:

| Intent | Sökterm | Sida |
|--------|---------|------|
| Jämförelse | "spel som 0-100" | `/spel-som-0-100` |
| Jämförelse | "alternativ till 0-100" | `/spel-som-0-100` |
| Online | "0-100 online" | `/spel-som-0-100` |
| Online tillsammans | "frågespel online tillsammans" | backlog `/fragespel-online` |
| Engelsk vinkel | "More or Less spel" | backlog |
| Brand | "Mer eller Mindre" (spel) | `/` |

---

## 3. Teknisk grund — status (LIVE / kodat)

- [x] `MainLayout.razor` — `<title>`, meta description, `rel=canonical` (absolut,
  host-byggd), Open Graph (type/site_name/locale/title/description/url/image), Twitter
  `summary_large_image`, favicon/apple-touch/manifest, `theme-color`, WebSite +
  Organization JSON-LD i `@graph`, valfri per-sida `JsonLd=` (FAQPage/Game).
- [x] `wwwroot/robots.txt` — `Allow: /` för alla + explicit allow för GPTBot,
  OAI-SearchBot, ChatGPT-User, ClaudeBot, Claude-Web, PerplexityBot, Google-Extended;
  `Sitemap:`-rad.
- [x] Dynamisk `/sitemap.xml` (`GameEndpoints.cs`) — `/`, `/om-spelet`, `/om-mig`,
  `/spel-som-0-100` + en `/games/new/{packId}` per pack.
- [x] `wwwroot/llms.txt` — sammanfattning + sidlista (Om spelet, Välj kviss,
  Spel som 0-100).
- [x] Content-sidor med FAQPage-schema: `/om-spelet` (`Components/OmSpelet.razor`),
  `/spel-som-0-100` (`Components/SpelSom0100.razor`); `/om-mig`.
- [x] Egen 404-sida; PWA-manifest + service worker; https via fly-edge.
- [x] Plausible privacy-analytics (game_created/joined/started/completed).

---

## 4. GEO-taktik (citerbarhet i AI-svar)

- [x] **AI-crawler-allow** i robots.txt (se §3) — ingen anmälningsportal, bara tillåtelse.
- [x] **llms.txt** — kortfattad sajtsammanfattning + sidlista för LLM-upptäckt.
- [x] **JSON-LD** FAQPage (Om spelet, Spel som 0-100) + Game — strukturerad data AI/sökmotor
  kan citera och Google kan visa som featured snippet.
- [x] **Jämförelsetabeller** i content (featured-snippet-vänligt format).
- **Mönster för ny content (citerbarhet):** naturliga frågerubriker (`<h2>` som en fråga)
  följt av kort, fristående definitionssvar i första meningen. AI-svar lyfter gärna den
  meningen.

---

## 5. Manuell anmälning (KVAR, ej gjort)

Flyttat hit från tasks.md. Verifiering via Cloudflare DNS TXT (hela domänen, ingen
kodändring).

### Google Search Console
- [ ] Skapa Domain-property `kvissig.se` på https://search.google.com/search-console
- [ ] Verifiera via DNS TXT i Cloudflare (klistra in `google-site-verification=...` på apex)
- [ ] Skicka in `https://kvissig.se/sitemap.xml`
- [ ] Begär indexering av `/`, `/om-spelet`, `/spel-som-0-100` (URL-inspektion)

### Bing Webmaster Tools
- [ ] Lägg till sajten på https://www.bing.com/webmasters
- [ ] Importera från GSC (snabbast) ELLER verifiera via Cloudflare DNS TXT
- [ ] Skicka in `https://kvissig.se/sitemap.xml`

### Uppföljning efter GSC
- [ ] Följ "spel som 0-100" / "alternativ till 0-100"-queries i Search Console och
  utvärdera om 0-100-vinkeln drar trafik (se §7).

---

## 6. Content-backlog (prioriterad)

Backlog = **idéer, inte beställning**. Varje sida är ett eget litet implementationsjobb och
följer ALLA samma mönster som `SpelSom0100`/`OmSpelet`: MainLayout + `.wrap.content` +
`FaqJsonLd` (const string via `JsonLd=`) + endpoint i `GameEndpoints.cs` + slug i
sitemap-urls-listan + llms.txt.

1. **`/fragespel-online`** — "frågespel online tillsammans"-intent. Hög prio: bred,
   icke-PlayMIG-beroende long-tail.
2. **`/spel-som-more-or-less`** — engelsk/internationell vinkel ("More or Less spel").
3. **Per-pack-/temasidor** — landningssidor per kviss/tema när korpus växer (t.ex. en sida
   per pack-slug).
4. **Utbyggd intern länkning** mellan content-sidorna (OmSpelet ↔ SpelSom0100 ↔ nya sidor)
   — sprider auktoritet, hjälper crawl.

---

## 7. Mätning & uppföljning

- **Plausible-mål:** funnel game_created → joined → started → completed (redan mätt).
- **GSC-queries:** följ long-tail-termerna i §2, särskilt 0-100-vinkeln.
- **Checkpunkt:** efter att `/spel-som-0-100` är indexerad — utvärdera om jämförelse-
  vinkeln genererar impressions/klick. Om noll efter rimlig tid: omprioritera backlogen mot
  `/fragespel-online`-intent istället.

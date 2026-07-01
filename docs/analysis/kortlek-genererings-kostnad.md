---
title: Kostnadsuppskattning per kortlek
type: analysis
status: uppskattning (ej mätning)
date: 2026-06-29
tags: [kostnad, agenter, kortlek, tokens]
---

# Kostnadsuppskattning per kortlek

**Kort svar:** grovt $150–800 per 1085-kortlek, troligast ~$300–500 (ca 3 000–5 000 kr).
Här är resonemanget bakom spannet.

## Observerat denna session (per agent-körning, total_tokens)

- frågesättare ~49k, faktagranskare ~48k, språkgranskare ~17k, tydlighetsgranskare ~17k
  → en batch (~27 kort) ≈ 130k tokens i ren kontextstorlek.
- Rebalanseringen lade på ~56k+ (cap-fix). Faktagranskaren körde dessutom ~87 min med
  ~19 WebSearch-turer.

## Skalning till 1085 kort

1085 / ~27 ≈ ~40 batchar × 4 agenter = ~160 agent-körningar, plus orkestratorn som läser
rapporter varje varv.

## Varför spannet är brett

1. De rapporterade total_tokens är ungefär agentens kontextstorlek, inte den fakturerade
   summan. Multi-turn-agenter (särskilt faktagranskaren med många WebSearch-turer) skickar
   om kontexten varje verktygsanrop → verklig fakturering blir 3–10× högre.
2. Prompt-caching är den största hävstången: cache-läsning ~$1,5/M vs färsk input $15/M.
   Med bra caching rasar inputkostnaden.
3. WebSearch-volym + rebalanseringar (som dinosaurie-cap nyss) varierar kraftigt per tema.

## Räkneexempel

Opus-prissättning ~$15/M in, ~$75/M ut som proxy — exakt 4.8-pris kan ej bekräftas:

- **Naivt** (rapporterade tokens, lite caching): ~40 × 130k ≈ 5M tokens → ~$100–150.
- **Realistiskt** (multi-turn-fakturering, måttlig caching): ~20–40M tokens → ~$300–500.
- **Högt** (WebSearch-tungt, många rebalanseringar, svag caching): ~60–80M tokens → ~$600–800.

## Slutsats

Räkna med $300–500 (~3 000–5 000 kr) per full kortlek som troligt utfall, med ytterkanter
$150 (snålt + bra caching) till $800 (search-tungt). Output-tokens (det dyra) är relativt
få här eftersom korten är korta CSV-rader — det drar ner kostnaden jämfört med en kodtung
uppgift. Mini-lekarna (175 kort) blir ca 1/6 av detta, dvs ~$25–80 styck.

**Notera:** detta är en storleksordnings-uppskattning, inte en mätning. För en exakt siffra
är säkraste vägen att läsa av faktisk token-förbrukning i API-konsolen efter en färdig kortlek.

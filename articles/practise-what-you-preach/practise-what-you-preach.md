# Practise What You Preach

> En 47 år gammal kostnadsmodell prissätter min helg till $492 788.

## 1. Ankaret

En 47 år gammal kostnadsmodell prissätter min helg till **$492 788**.

Jag körde `scc` — ett verktyg som räknar rader kod — på ett multi-player quizspel jag byggde
solo på en helg. Det spottade ut COCOMO-skattningen nedan. Cost inringad i magenta, för
säkerhets skull:

![[assets/cocomo.png]]

Nästan en halv miljon dollar. **10,51 månader. 4,17 personer.** För en helg, ensam.

Innan vi går vidare: lägg märke till vad den siffran just gjorde med dig. Du har nu en
referenspunkt. Allt jag säger härnäst kommer du omedvetet att mäta mot en halv miljon
dollar. Det är inget olycksfall — det är ett **ankare** (Kahneman & Tversky: vi klamrar
oss fast vid det första talet vi ser, hur godtyckligt det än är). Jag namnger det med
flit, så att jag är med på skämtet och inte offer för det. Siffran är fel. Men titta på
vad den gör med din förväntan.

Och innan någon ingenjör i baksätet sätter sitt eget mot-ankare — *"pfft, ett
vibe-codat helgleksak, värt typ $0"* — nej. Det är lika fel, åt andra hållet. Den här
texten handlar om vad som ligger emellan, och varför avståndet inte mäts i rader kod.

## 2. Ankaret ljuger åt två håll

COCOMO (Constructive Cost Model, Barry Boehm, **1981**) skattar kostnad ur en enda
ingång: antal rader kod, körda genom en takt kalibrerad mot vattenfallsprojekt på
stordatorer. Den ljuger på två sätt samtidigt.

**Den inflaterar.** Av de 18 988 raderna `scc` räknade är massor inte handskriven
applikationslogik: 5 306 rader CSV-frågedata, 4 542 rader Markdown (specs, ADR:er, den
här sortens dokument), genererad och konfigurerad kod. Och även den äkta koden prissätts
i 1981 års takt — en epok utan ramverk, pakethanterare eller en standardbibliotek som
gör det tunga lyftet.

Skala ner till **enbart ren kod** — C#, Razor, CSS — och modellen säger:

> **$126 384 · 6,27 månader · 1,79 personer** (4 347 rader kod)

Mer ärligt. Fortfarande absurt för en solo-helg.

**Den är blind.** Det COCOMO *inte* kan se är där allt arbete faktiskt ligger:
specifikationen som är sanningskälla, de sju arkitekturbesluten, testdesignen,
agent-pipelinen som granskar frågedatan. Och inte bara det osynliga *tankearbetet* — också
det konkreta arbete som inte är applikationskod alls. **1 085 frågekort**, vart och ett
författat, faktagranskat mot källa och språkputsat (§5) — modellen ser 5 306 textrader att
prissätta som om de vore kod, men inte timmarna av kurering bakom dem. Och allt som krävdes
för att det här ens skulle finnas på en adress: köpa domänen, konfigurera DNS, skriva
CI/CD-pipelinen i GitHub Actions, deploya till fly.io. Noll rader, i modellens ögon. Den
största delen av arbetet väger ingenting.

Det är pudelns kärna: **modellen mäter skuggan, inte det som kastar den.** Rader kod är
skuggan. Det som kastar skuggan — övad metod — syns inte i en SLOC-räknare.

## 3. Vad som faktiskt gjorde det snabbt: övad metod, inte AI-typing

Hävstången var inte att jag skrev fort, och inte att en AI skrev åt mig. Den var att
*besluten redan var fattade* — av en uppsättning seniora discipliner jag har **övat**
tills de blivit reflex. Tre av dem är ryggraden, och de landar alla i samma form:
**Given–When–Then**.

### Vertical Slice Architecture

Bygg en funktion åt gången, hela vägen genom: en self-contained slice. Ingen kod
delas spekulativt mellan funktioner — varje slice är ett **oberoende verifierbart
Given–When–Then-kontrakt**.

![[assets/vertical-slice.svg]]

För ledaren: du kan leverera och verifiera en funktion utan att rota i tio andra. För
ingenjören: ingen prematur abstraktion, inga lager-för-lagrets-skull, koppling hålls
inom slicen.

### Functional Core / Imperative Shell

All beslutslogik bor i en **ren kärna** — inga databaser, ingen klocka, inget nätverk,
helt deterministisk. Allt stökigt (I/O, tid, anrop) trycks ut i ett tunt yttre skal.

![[assets/functional-core.svg]]

Konsekvensen står i bilden: *beteendet är simulerbart — kör tusentals scenarier utan
databas, verifierbart innan det ens är byggt.* Du behöver inte starta något för att veta
att logiken stämmer. Det är därför testerna är bombsäkra (mer om det strax).

### Decider + Event Sourcing

Spelet är en **Decider**: två totala funktioner.

```
decide:  (State, Command)  →  Result<Event[]>
evolve:  (State, Event)    →  State
```

![[assets/decider-pattern.svg]]

Loopen *är* Given–When–Then: **Given** ett tillstånd (vikta tidigare events via
`evolve`), **When** ett kommando, **Then** events — eller en avvisning. Samma form som
slicen, samma form som testet. Designen och testet talar samma språk. (Inledande och slutliga tillstånd utelämnade för läsbarhet.)

### Stödbalkarna

Det här är inte tre lösryckta mönster — de hänger på en gemensam ställning:

- **Spec som sanningskälla.** Allt beteende definieras först i en emlang-spec
  (event modeling, ADR 004) — inte i koden, inte i mitt huvud. Koden följer specen.
- **Sju ADR:er.** Varje vägval (event sourcing in-memory, Decider, HTMX-polling, CSV,
  ROP, Razor static SSR) är ett skrivet beslut med ett *varför*. Inget omförhandlas i
  varje commit.
- **Result / Railway-Oriented Programming** via native unions (ADR 006). Affärsfel är
  *värden på ett felspår*, aldrig kastade undantag. Kärnan förblir total.
- **Constraints som feature.** Ingen SignalR, ingen Entity Framework, ingen
  Blazor-circuit. Färre rörliga delar = färre fellägen. Begränsningen är designen.
- **Determinism via `GameContext`.** Klocka och id-generator *injiceras*. Därför är
  kärnan deterministisk, och därför är FC-testerna bombsäkra — rena och förutsägbara,
  helt utan mocks.

Inget av det här är talang. Det är *övad ingenjörsdisciplin*. Det är repetition tills besluten
sitter i ryggmärgen och beslutskostnaden går mot noll.

## 4. Kvalitet påstås inte — den upprätthålls

"Snabbt och billigt" betyder ingenting om det går sönder. Så det här är inte ett
påstående om kvalitet — det är en *mekanism* för den.

- **Bombsäkra FC-tester.** Given–When–Then- och Given–Then-fall i `DeciderTests`,
  `EvolveTests`, `ProjectionTests`. Den rena kärnan gör dem deterministiska och
  mock-fria — testet kör samma loop som designen.
- **Arkitekturtester.** `ArchitectureTests` upprätthåller FC/IS-gränsen *i CI*. Om någon
  smyger in I/O i kärnan failar bygget. Disciplinen är automatiserad, inte hoppfull.
- **End-to-end.** `GameEndpointsTests` mot en `TestAppFactory` kör hela vägen genom det
  imperativa skalet.
- **EvalOps.** När ett test fallerar matas det tillbaka in i minnet — felet blir en
  permanent läxa, inte en upprepad miss.
- **Context management som mekanism.** En `CLAUDE.md`, en constitution, ett auto-minne.
  Inte "jag försöker komma ihåg" — en skriven, laddad kontrakt-yta. Mekanismen, inte
  bara intentionen.
- **Förenkling som inte behövs.** Jag kör ponytail i Claude Code — en agent vars enda
  jobb är att jaga överarbete och föreslå det enklaste som fungerar. Poängen: den hittar
  nästan aldrig något. Vertical slices, functional core och constraints-as-feature
  förebygger bloat *vid källan* — komplexiteten skrivs aldrig, så det finns inget att
  städa bort i efterhand. Den billigaste förenklingen är den man aldrig behövde göra.

## 5. Och inte bara kod — content också

Frågedatan är inte hopkastad. Varje kort passerar en pipeline av specialiserade agenter
innan det når den live-paketet:

![[assets/agent-pipeline.svg]]

**frågesättare** författar batchar mot svårighetsbanden → **faktagranskare** verifierar
*varje* värde och riktning mot källa och pinnar årtalet (overifierbart = förkastat) →
**språkgranskare** putsar svenskan utan att röra en enda siffra → **kurator**
(`tools/pack.cs merge`) dedupar och kontrollerar bandhistogrammet in i live-paketet.

Hög kvalitet, stor människa/AI-insats — och **noll rader kod**. Lika osynligt för COCOMO
som arkitekturen. Content är hantverk på samma villkor som mjukvaran.

## 6. Vad som kollapsar — och vad som blir kvar

Återvänd till hävstången i §3: den var aldrig tangenttrycken. När metoden är övad och
besluten fattade kollapsar själva *kodandet* mot noll — skrivandet av rader är det sista,
billigaste steget, inte arbetet. COCOMO prissätter exakt det som kollapsade (rader kod) och
är blind för det som blir kvar. Samma "skugga vs. det som kastar den" som i §2 — nu vänt
framåt: skuggan krymper, men det som kastar den gör det inte.

När kodandet kollapsar flyttar värdet. Två vägar blir kvar.

**Product engineer.** Den som på djupet förstår *vad* som ska byggas och *varför* — vilket
problem, för vem, vad som ska skippas. Det är omdöme, inte syntax, och det överlever varje
modellgeneration. ACMM landar i samma slutsats: *"the decision about what to build and what
to skip — that's still me."* Modellen kan implementera; den kan inte bestämma vad som är
värt att bygga.

**Elite engineer.** En av få som bygger själva *fabriken* — agenterna, harnesset,
pipelinen som producerar och granskar arbetet. ACMM:s kärnfynd: *"the intelligence is in
the system, not the model."* Modellen är en commodity; infrastrukturen runt den är
differentieringen. Att byta modell tar en eftermiddag; att bygga om systemet runt den tar
månader.

Det är ett hävstångsskifte. Förr multiplicerade en force-multiplier *genom människor* —
staff+-ingenjören med starka people skills som lyfter ett team. Stark hävstång, men taket
sitter i antalet människor man kan nå: kanske 10x. Att multiplicera *genom agenter, harness
och pipeline* har inte samma tak — riktningen pekar mot 100x, 1000x. Riktning, inte ett mätt
löfte; var x faktiskt landar är en empirisk fråga, inte en utfästelse (jfr kvalitetsnoten
nedan — vi påstår inte siffror vi inte mätt).

Och kvissig är beviset i miniatyr. Content-pipelinen (§5) *är* en sådan fabrik: agenter som
författar, faktagranskar, putsar och kurerar utan att jag skriver en rad. Metoden (§3) och
mekanismen (§4) *är* precis vad som blir kvar när kodandet kollapsar — de besluten, den
disciplinen, det som kastar skuggan.

## 7. Practise what you preach

Skickligheten *är* predikan, och predikan är övad. Det var aldrig AI:n och aldrig
tangenttrycken — det var att besluten redan var fattade, av discipliner jag tränat tills
de blivit billiga. Metoden är multiplikatorn.

Och här är meta-beviset: den här artikeln är själv ett exempel på tesen. Argumentet,
illustrationerna — hämtade ur mitt eget mönsterbibliotek, samma visuella språk som
spelets diagram — och draftandet följde exakt samma övade metod som koden.

Ankaret var aldrig poängen. Disciplinen är det.

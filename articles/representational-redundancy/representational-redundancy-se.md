# Representationell redundans

> I fredags raderade jag 557 rader C# ur git. Bygget blev grönt.

## Raderna som inte saknas

I fredags raderade jag 557 rader C# ur git. Bygget blev grönt. Alla 181 tester gröna.
Webben orörd.

Raderna var record-lagret i tre spel (kommandon, events, fel) och de raderades för
att de inte längre behövs *som filer*. De genereras vid varje bygge, deterministiskt,
ur samma emlang-spec som alltid varit sanningskällan, av en Roslyn source generator
rakt in i kompileringen. De finns aldrig på disk. De kan inte drifta, för det finns
inget att drifta *från*.

Och här är siffran som avslöjar vad det egentligen handlar om: nettot i git blev
**+7 rader**. Borttagen transkription: ~701. Tillagd generator-infrastruktur: 708.
Radräkningen är ett nollsummespel. Så varför bry sig?

För att raderna aldrig var poängen. Poängen är vad de 557 raderna *var*: samma fakta,
sagda en gång till.

## Fienden har ett namn

Hur många ställen i din kodbas vet att en spelare har ett namn?

Entiteten. DTO:n. Mappern däremellan. SQL-kolumnen. Migrationen som skapade kolumnen.
Validatorn. OpenAPI-schemat. TypeScript-interfacet. Test-fixturen. Nio representationer
av **ett** domänfaktum, och varje beteendeändring är en koherent redigering på nio
ställen samtidigt.

<!-- TODO asset: fan-out-diagram: ett faktum, nio representationer, samma visuella språk som decider-pattern.svg -->
![[assets/one-fact-nine-places.svg|700]]

Det här förtjänar ett eget namn: **representationell redundans**. Inte "lager", inte
"boilerplate", utan omsägning. Samma sanning transkriberad för hand mellan representationer
som ingen kompilator håller ihop.

Människor har alltid driftat på det här: det är därför "dokumentationen ljuger" blev
folklore. Men lägg märke till exakt vad LLM-agenter är *sämst* på: koherent redigering
på många ställen. En agent producerar plausibel kod på varje enskilt ställe; det är
*mellan* ställena det brister. Representationell redundans är alltså inte bara dyr som
förr, den är dyr precis där den nya arbetskraften är svagast.

Deterministisk härledning *hanterar* inte den redundansen. Den **raderar** den.

## Det var aldrig lagren

Nu invändningen jag själv skulle ropa från baksätet: *"jaha, så arkitektur är onödigt
nu, bara YOLO:a allt i en fil?"* Nej. Tesen är inte anti-lager.

Det här repot behåller den hårdaste gränsen som finns (functional core / imperative
shell) och upprätthåller den i CI med arkitekturtester. Beroendedisciplin *hjälper*
agenter: liten sprängradie, testbara sömmar, en regel ("kärnan rör inte IO") som failar
bygget när den bryts.

Skilj alltså på två saker som klumpas ihop i varje clean/onion/n-tier-diskussion:

- **En gräns** uttalar en *regel*, en gång: beroenden pekar ditåt, aldrig hitåt.
- **En nivå** som säger om samma faktum (entity till DTO till mapper till schema)
  uttalar ingen regel alls. Den transkriberar.

Gränser är billiga och maskinellt kontrollerbara. Omsägning är dyr och maskinellt
okontrollerbar. Konventionella lagerarkitekturer med ORM institutionaliserar
omsägningen och kallar den disciplin.

## Vem transformerar?

"Programmera på engelska" är tidens melodi: vibe coding, spec-kit:ar, spec-först-IDE:er.
Och de har rätt om halva saken: intentionen, inte koden, borde vara den beständiga
artefakten. Men titta på vem som utför transformationen från intent till kod i varje
sådant upplägg: **en LLM**. Markdown in, probabilistisk kod ut. Varje regenerering är
ett nytt stokastiskt utfall; diffar komponerar inte; varje körning är en ny
granskningshändelse. Spec-driften man skulle bota flyttar in i spec:en själv.

Det finns en trappa, och stegen skiljer sig åt i *vem som transformerar och vad som
verifierar*:

1. Engelska → LLM → kod, människan granskar allt. (Vibe coding: även myntaren
   avgränsade det till slit-och-släng.)
2. Markdown-spec → LLM → kod + tester. Intent fångad, transformationen fortfarande
   stokastisk.
3. **Formell spec → deterministisk generator för det bevisbara skiktet + agent som
   skriver resten mot kompilator- och testorakel.** ← hit flyttade jag i fredags.
4. Fullständig formell syntes. (Ingen seriös gör anspråk.)

<!-- TODO asset: trappan, fyra steg, transformer + verifierare per steg -->
![[assets/transformer-ladder.svg|700]]

Varje steg uppåt köper *granska-en-gång*-semantik för ett större skikt. Generatorn
granskades en gång och bevisades mot alla tre spelen, därefter är regenerering ett
byggsteg, inte en granskningshändelse. Samma spec in, byte-identisk kod ut. CI kan
hävda `artefakt == f(spec, generator)` som en invariant, inte som en förhoppning.

Dijkstra sa det 1978, om idén att programmera i naturligt språk: formella texter är
effektiva just för att deras legitimitet kan kontrolleras med några få enkla regler,
medan naturligt språk utmärker sig i att göra nonsens icke-uppenbart. Fyrtioåtta år
senare är det fortfarande hela skillnaden mellan steg 2 och steg 3.

## Agenten är en förstärkare

Det dyra i agentisk utveckling är inte längre att producera kod, utan att *veta att
den är rätt*. Verifiering är flaskhalsen. Då är den intressanta egenskapen hos en
arkitektur dess **orakeltäthet**: hur snabbt och hur mekaniskt upptäcks fel?

I det här repot: ett nytt event i spec:en blir ett kompileringsfel på varje ställe som
måste hantera det (uttömmande unions, inga default-armar, warnings som errors). 181
rena Given–When–Then-tester kör på under åtta sekunder: inga mocks, ingen databas,
inga containrar, för kärnan är två totala funktioner. Jämför loopen i en lager-stack:
migrationer, testcontainrar, minuter per varv, och de viktigaste felen upptäcks i
runtime, där agentens återkopplingsloop är som svagast.

Så här landar tesen, och det är inte en smaksak: **agenten är en förstärkare av den
verifieringsregim som redan finns.** Förstärkt diffus verifiering ger plausibel drift i
maskinfart. Förstärkt koncentrerad verifiering ger kontrollerbara inkrement i
maskinfart. Arkitekturen väljer vilket.

## Där jag kan ha fel

Tre ärliga hål, innan någon annan hittar dem åt mig.

**MDA-spöket.** 1:1 modell→kod med genererade artefakter utanför versionskontrollen är
*exakt* vad Model-Driven Architecture lovade på 00-talet, och det dog: handredigeringar
bröt rundresan, modellerna blev lika otympliga som koden, sista 20 % fick aldrig plats.
Skillnaderna som måste förbli sanna här: determinismen **bevisades innan** något
flippades (skuggtester, noll avvikelser, tre spel); genererad kod kan inte
handredigeras *per konstruktion* (den finns bara i kompileringen); och flyktventilen är
en typad söm där saknad mänsklig kod är ett kompileringsfel, inte en "protected
region" som tyst ruttnar.

**Träningsdatan.** LLM:er är som mest flytande där träningsdatan är tjockast:
mainstream-CRUD i lager. En projektlokal spec-dialekt är en zero-resource-DSL: agentens
råa flyt är som högst precis där jag påstår att arkitekturen är som sämst. Motmedlet är
att repot bär sin egen instruktionsuppsättning (spec-lathund, constitution, ADR:er,
fitness-tester), en fast kontextkostnad i stället för spridda läsningar per ändring.
Tre felfria transkriptioner och två avvikelsefria flips säger något. Men det är
anekdot, inte mätning. Ingen har publicerat benchmarken.

**Min egen läs-sida.** Ett faktum passerar fortfarande spec → record → projektion →
vy-modell → Razor hos mig. Fyra, fem representationer. Tesen är bara delvis realiserad
i sitt eget skyltfönster. Nästa steg i experimentet riktar sig dit, och om sömmarna
då börjar samla handunderhållen metadata tills spec-granskning kostar mer än
kodgranskning, då har MDA-spöket vunnit och jag skriver den artikeln också.

## Kontraktet, inte raderna

Om agentgenererad kod är billig och regenererbar, varför då bygga maskineri för att
hålla 557 rader ute ur git? För att maskineriets värde aldrig var raderna. Det är
**kontraktet**: determinism gör regenerering till ett byggsteg i stället för en
granskningshändelse, och gör spec:en till den enda ändringsytan för hela skiktet,
upprätthållet av kompilatorn i stället för av disciplin.

Hävstången för agentisk utveckling är alltså inte "spec i stället för kod", och inte
"färre lager". Den är: **maximera andelen av systemet vars korrekthet avgörs
maskinellt, och minimera representationell redundans i resten.** Programmera-på-engelska
över en lager-och-ORM-stack är svag på båda axlarna samtidigt: transformationen är
stokastisk och verifieringen är utsmetad. En formell spec med deterministisk generator
och en ren, uttömmande kärna är stark på båda.

Fienden hade ett namn hela tiden. Det var bara inte "lager".

## Spela

Teorin bor i ett spel, och spelet är till för att spelas. Samla gänget, öppna
[kvissig.se](https://kvissig.se) och se vem som gissar närmast. Mer eller mindre?

# PREDMET - Návrh a vývoj počítačových hier

## Členovia tímu
* Alex Duchoň
* Tomáš Zenka
* Krištof Kmeť
* David Vach

## Dizajn
* Jakub Ralbovský

## Verzia a nastavenie
* Projekt je robený v Unity verzia **6000.6.3.f1**

# Reticulum

Reticulum je ťahová roguelike RPG hra, v ktorej je každý súboj priestorovou hádankou. Hráč ovláda hrdinu na ceste za oslobodením ostrova, prechádza procedurálne generovanou mapou a využíva unikátny mriežkový (grid-based) súbojový systém. 

**Žáner:** RPG, Turn-Based, Roguelike, Strategy
**Platforma:** PC
**Art Style:** Pixel art

## Príbeh
Ostrov Reticulum je domovom štyroch zvieracích klanov (kobry, mloky, medvede, sovy), ktoré ovládajú prírodné živly – oheň, vodu, zem a vzduch. Zradný mlok uvalil na ostrov kliatbu, ktorá zmrazila živly obyvateľov do podoby geometrických kryštálov v ich telách a spravila z nich bábky. Hráč ako jeden z mála prežil s vlastnou slobodnou vôľou. Zistil, že toto prekliatie mu dáva novú silu, vďaka ktorej môže oslobodiť ostatných.

## Kľúčové mechaniky

* **Mriežkový súbojový systém (Grid Combat):** Zdravie hráča aj nepriateľov je reprezentované mriežkou. Zbrane majú vlastné geometrické tvary, ktoré sa počas útoku prikladajú na mriežku nepriateľa. Pokryté políčka prichádzajú o životy a kryštály sa trieštia.
* **Taktické plánovanie:** Súboj je ťahový. Hráč využíva Staminu na útoky a predmety. Nepriatelia dopredu vizuálne signalizujú svoje úmysly, čo umožňuje presne naplánovať útok aj obranu.
* **Elementárne synergie:** Všetky postavy a predmety sú viazané na jeden zo štyroch živlov. Využívanie správnych elementov proti nepriateľom prináša bonusy a mení dynamiku boja.
* **Obchodovanie za krv:** V hre neexistujú klasické peniaze. Pri stretnutí s obchodníkom (cestujúcou pandou) sa za vylepšenia a predmety platí trvalým odstránením políčok z vlastnej mriežky maximálnych životov.
* **Roguelike štruktúra (Permadeath):** Hra sa delí na 3 hlavné zóny (Forlorn Fields, Desolate Districts, Luminous Lake) a finálnu oblasť (Garbled Ground). Každá vrstva je zakončená unikátnym boss fightom. Ak hráč príde o celú svoju mriežku životov, run končí.


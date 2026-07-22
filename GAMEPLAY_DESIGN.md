# The Call of a Cave — Three-Minute Colony Specification

## Vision

A science-fiction pixel-art colony game about raising cave bugs in a barren underground settlement. The left side grows into a camp-city while the right side becomes an invasion front. Bugs do not obey movement orders: every unassigned bug wanders across the full cave, so protecting valuable population is part of the strategy.

## Time Scale

One colony year equals **5 real-time seconds**. A normal bug lives for **36 colony years / 180 real-time seconds / 3 minutes**.

| Fixed timer | Value | Design purpose |
| --- | ---: | --- |
| Egg incubation | 15 s | Opening eggs hatch before the first larvae mature. |
| Juvenile stage | 30 s | Ages 0–6. The player has a short setup window. |
| Adult stage | 120 s | Ages 6–30. Main work, breeding and training window. |
| Elder stage | 30 s | Ages 30–36. Final warning window before pollution-producing death. |
| Normal lifespan | 180 s | Complete three-minute individual lifecycle. |
| Health loss tick | 1 health / 2.5 s | 72 health lost over an untreated normal life. |
| First egg delay | random 35–60 s after adulthood | Prevents synchronized population bursts. |
| Later egg interval | 60 s | A free adult normally lays 1–2 eggs during adulthood. |
| Factory level 1 production | 8 s per worker | Produces stackable scrap for steady opening income. |
| Factory level 2 production | 7 s per worker | Produces higher-value refined components. |
| Factory level 3 production | 12 s per worker | Produces sellable/equippable elite exoskeletons. |
| Soldier training | 10 s | Exactly two colony years. |
| Hospital cure | 5 s | Infection remains dangerous but treatment fits the short loop. |
| Academy training | 10 s | Doctor response can happen inside the same crisis. |
| Pollution cleaning | 1 s | Doctor must reach the source; cleanup itself stays brisk. |
| First invasion | 90 s from game start | Divides the lifecycle into setup and danger halves. |
| Invasion wave interval | 45 s | Creates a repeated pressure cycle without interrupting every task. |
| First heavy invader | First wave after 300 s (normally 315 s) | Keeps the first five minutes readable before escalation. |
| Combat attack interval | 0.75 s | A normal robot fight resolves in roughly 7–9 seconds. |

Additional fixed timing:

- Autonomous wander direction changes every randomized 1.08–2.52 seconds (1.8-second base × 0.6–1.4).
- Pollution checks nearby bugs every 0.35 seconds.
- Hit flash, shake, and knockback feedback lasts 0.22 seconds.
- UI panels fade over 0.18 real-time seconds.
- Toasts hold for 1.6 real-time seconds before fading out.
- Block transitions wait 0.02 seconds after covering and 0.05 seconds at the dark midpoint.

Time multipliers:

- Health below 20: aging ×2 and factory efficiency ×0.5.
- Infection: health loss ×2 and aging ×2.
- Active soldier combat: aging ×1.5.
- Multipliers stack.
- Factory workers and soldiers retire automatically when they reach the elder stage; retirement restores the Free role.

## Opening State

- 300 coins.
- 3 juvenile bugs in the open nursery.
- 2 eggs in the nursery; they hatch at 15 seconds.
- No adults and no food stock.
- The factory begins as a fixed unbuilt site. Barracks, hospital, and medical academy do not exist until placed from the construction palette.
- Quicksand and the sand-river hazard are not part of this version.

## Economy

There is no passive income and eggs cannot be sold. Factory cargo is the primary money source, so assigning adults to production sacrifices future eggs in exchange for economic growth. Purchased food and factory products enter the backpack; food becomes a loose world entity only when the player drags it out.

| Item or action | Cost / reward |
| --- | ---: |
| Buy juvenile | 100 coins |
| Buy one food | 30 coins |
| Drop one food | 1 food; the first bug to reach it restores 55 hunger and 35 health |
| Sell level 1 metal scrap | +80 coins |
| Sell level 2 refined component | +130 coins |
| Sell level 3 elite equipment | +240 coins, or equip one soldier |
| Train one doctor | 150 coins + 3 food |

### Economy pacing target

- Building the factory immediately leaves 200 coins.
- The first 3 larvae mature at 30 seconds; the 2 opening eggs become adults at 45 seconds.
- Two healthy level-1 factory workers operating from 30 to 90 seconds produce about 14 scrap, worth roughly 1,120 coins.
- That supports a barracks and hospital before the first invasion, or an academy-focused route, but does not fund every building and upgrade at once.
- A single full-adult-stage level-1 worker can produce about 15 scrap / 1,200 coins. The same free adult would instead lay roughly 1–2 eggs.
- Level 2 raises per-worker sale income from 10 to about 18.6 coins/second. Level 3 only raises it to 20 coins/second because its product also carries major combat utility.

## Bug Life, Health, and Roles

| Stage | Real time | Colony age | Rules |
| --- | ---: | ---: | --- |
| Juvenile | 0–30 s | 0–6 | Wanders; cannot work or train. |
| Adult | 30–150 s | 6–30 | Can breed or be assigned to a facility. |
| Elder | 150–180 s | 30–36 | Uses the elder sprite and leaves pollution on death. |

Every standard bug instantiates the reusable `CreatureInfoPanel.prefab`. The 460×224 screen-space card is hidden by default and appears only while that bug is under the mouse. Its four lines show permanent code, integer colony age, satiety, and role. Age begins at 1 and rises by 1 every 5 real-time seconds. Satiety is a positive 0–100 value: higher means fuller, and eating raises it. The code is rolled once at spawn as two uppercase English letters plus three digits (for example `QF-027`) and never changes during that bug's life. Juveniles, unassigned adults, and elders use the Free role label.

- Health starts at 100.
- Health 50–100 is green, 20–50 yellow, and below 20 red.
- Hunger rises continuously. Low satiety accelerates passive health loss.
- Food is never applied directly to a bug. The first eligible bug to touch dropped food restores 55 hunger and 35 health.
- Hits flash the bug red, shake it, and apply light knockback.
- The nursery is only a spawn point. All unassigned juveniles, adults, elders, and soldiers wander over the complete map.
- Constructed facilities are walls to autonomous bugs; the nursery remains open.

### Reproduction trade-off

- Only an unassigned normal adult lays eggs.
- Factory workers, soldiers, barracks trainees, hospital patients, and academy workers do not lay eggs.
- First egg: 35–60 seconds after becoming adult; later eggs: every 60 seconds.
- Eggs hatch after 15 seconds.
- Maximum live bugs: 40; maximum simultaneous eggs: 30.

## Disease and Pollution

- Elder death and soldier combat death leave mutated oil pollution.
- A bug touching pollution becomes infected.
- Infection doubles both health loss and aging speed.
- An infected bug is cured after 5 seconds in a hospital; penalties continue during treatment.
- A cured patient stays in the hospital until the player drags it out.
- A doctor cleans a pollution source after reaching it and working for 1 second.
- Doctors automatically seek pollution and return toward the academy when idle; the player can drag them.

## Facilities

All standard facilities use capacity **3 / 5 / 10** at levels 1 / 2 / 3. The academy instead trains **2 / 4 / 6 doctors per batch**. Facilities are hollow perimeter compounds: assigned bugs wander inside the central 48% of the courtyard radius and do not hug the walls.

### Construction grid and footprint

- Factory, barracks, hospital, and research academy are all dragged from the right command sidebar onto the world. No fixed factory placeholder remains on the map.
- Facilities unlock linearly: **Factory → Barracks → Hospital → Research Academy**. A locked card cannot begin placement.
- Placement snaps to a **0.5-world-unit grid** inside the editable construction region from **(-8.5, -4.5)** to **(3.5, 4.5)**.
- Every player-placed facility reserves its level-3 square footprint immediately: **4.4 × 4.4 world units**. Reserved footprints cannot overlap the nursery or another facility.
- Actual hollow-wall radii grow by level: **1.15 / 1.50 / 1.90 world units**. The reserved maximum footprint stays unchanged, so an upgrade never collides with a neighboring structure.
- Level 2 adds visible utility modules; level 3 adds command nodes and gold high-tier fittings. Upgrade readability is not scale-only.
- A green/red footprint preview and the normally hidden grid are shown only during a drag. Escape or right-click cancels placement.
- The nursery is not a building and has no wall. It is an open birth pit that wandering bugs can freely cross.

The battlefield itself is a scene-authored Unity Tilemap. Its Rule Tile chooses among four seamless sand variations on a 0.5-unit grid, while cave-edge tiles live on a separate editable layer. This is the same grid used by facility placement.

### Factory

| Level | Capacity | Build / upgrade cost |
| --- | ---: | ---: |
| 1 | 3 | 100 |
| 2 | 5 | 350 |
| 3 | 10 | 650 |

- Level 1: one metal scrap every 8 seconds; click its backpack stack to sell for 80 coins.
- Level 2: one refined component every 7 seconds; click its stack to sell for 130 coins.
- Level 3: one elite exoskeleton every 12 seconds; click to sell for 240 coins or drag it onto a normal soldier.
- Below 20 health: production speed is halved at every level.
- Factory assignment lasts through adulthood and uses the dedicated worker appearance. On reaching elder age, the worker automatically retires, exits the factory, and becomes Free.

### Barracks

| Level | Capacity | Build / upgrade cost |
| --- | ---: | ---: |
| 1 | 3 | 250 |
| 2 | 5 | 250 |
| 3 | 10 | 450 |

- Training takes 10 seconds / two colony years.
- Completion creates a soldier with 100 combat health. Soldiers keep the normal lifespan and automatically retire as Free elders.
- A level-3 factory exoskeleton can upgrade a soldier once. The elite soldier has 200 combat health and 20 damage, visually marked with a gold/teal equipment tint.

### Hospital

| Level | Capacity | Build / upgrade cost |
| --- | ---: | ---: |
| 1 | 3 | 400 |
| 2 | 5 | 250 |
| 3 | 10 | 450 |

- Infection cure: 5 seconds.
- Soldiers recover 8 combat health per second while inside.
- Patients remain until manually removed.

### Research Academy

| Level | Doctors per batch | Build / upgrade cost |
| --- | ---: | ---: |
| 1 | 2 | 700 |
| 2 | 4 | 900 |
| 3 | 6 | 1300 |

- Requires at least one assigned free adult.
- Training takes 10 seconds.
- Every doctor in the batch costs 150 coins and 3 food; costs multiply by batch size.

## Invasion and Combat

- The first scavenger robot enters from the right at 90 seconds; another wave arrives every 45 seconds.
- Wave schedule begins with 1, 1, 2, 2, and 3 normal robots at 90 / 135 / 180 / 225 / 270 seconds.
- Heavy enemies do not appear during the first five minutes. The 315-second wave contains 3 normal robots and 1 heavy invader.
- Later waves rise toward 5 normal robots. Heavy count grows from 1 to a maximum of 3, adding one roughly every 180 seconds after heavy enemies unlock.
- Soldier and robot attack interval: 0.75 seconds.
- Soldier: 100 combat health, 10 damage.
- Robot: 110 health, 11 damage.
- One soldier may win but is likely to die; two soldiers reliably defeat one robot.
- Elite soldier: 200 combat health, 20 damage. One elite soldier can narrowly defeat two normal robots if it begins at full health.
- Heavy robot: 330 health, 22 damage, three times the normal enemy's visual size, and lower movement speed. It is intended for multiple soldiers or elite support.
- A soldier ages ×1.5 while actively fighting.
- Combat health reaching zero kills the soldier and produces pollution.
- Every enemy continuously attacks the nearest visible valid target, whether that target is a bug or a constructed facility.
- A normal adult can fight automatically for 2 damage per strike, exactly one fifth of a standard soldier. Juveniles and elders die from a single enemy hit.
- Each facility level has structural health. Depleting it downgrades a level-2 or level-3 facility; depleting level 1 destroys the facility and releases every adult occupant as Free.
- Enemy death never creates pollution.

## Interaction Rules

- Drag an unlocked facility card from the right construction page to a valid grid footprint to build it and pay its cost.
- Shift-click a constructed facility to upgrade it.
- Click the academy to start a doctor batch when its requirements are met.
- Drag adults into facilities.
- Factory workers remain assigned until retirement; hospital patients can be removed; trained soldiers are released from barracks.
- Soldiers and normal adults engage nearby enemies automatically; no separate right-side deployment flag is required.
- The right-side backpack is a scrollable 3-column grid with a 3×3 visible window and 12 scene-authored slots. Empty slots show no phantom item icon.
- Build, Research, Loadout, backpack, and economy HUD share one opaque right sidebar outside the battlefield. Research exposes Medical Doctor plus three reserved future-talent sockets; Loadout reserves armor, weapon, and core sockets.
- The boot menu uses a full-screen cave illustration and a fixed English art logo. English hides the duplicate subtitle; every other locale displays only its own localized title below the logo.
- Purchased food enters the backpack. Drag it to any world position to create a dropped-food entity. If dropped inside a facility, only occupants of that facility race for it; outside, hungry unassigned bugs within the sensing radius move toward it. The first arrival consumes it.
- Factory products enter their matching stack. Click scrap, components, or equipment to sell one item.
- Drag elite equipment onto an unupgraded soldier to consume it and double that soldier's combat health and attack.
- All player-facing text must exist in Chinese and English in `LocalizationTable`.

## Session Length and Content Horizon

The simulation has no fixed victory timer. Reproduction lets a well-managed colony continue across multiple three-minute generations, while waves keep escalating.

The HUD clock is an independent two-row panel centered at the top of a 1920×1080 view, outside the resource card. `TIME` records the current run duration in `MM:SS` (or `H:MM:SS` after one hour). `YEAR` is the colony-growth calendar: it starts at `0001`, advances once per 5 real-time seconds, expands naturally to five digits after year 9999, and caps at `99999`. Both advance only while the game state is Playing, freeze during pause, and are repeated on the Colony Lost screen.

- **0–90 seconds:** build order, first adults, first production and defense choice.
- **90–300 seconds:** five normal-enemy waves, population replacement, facility upgrades and academy route.
- **315 seconds onward:** heavy enemies make elite equipment and coordinated defense relevant.
- **Current meaningful progression:** approximately 6–10 minutes for a first successful run to reach level-3 production and survive at least one heavy wave.
- **Current content repetition point:** approximately 10–15 minutes. The colony can mechanically survive longer, but enemy silhouettes, equipment choices and strategic events begin repeating.

Recommended next content material, in priority order:

1. Equipped elite-soldier idle/walk/attack sprite sheet instead of tint-only differentiation.
2. Normal enemy level 2 plus heavy enemy walk/attack/hit sheets.
3. Additional hand-authored upgrade variants for every facility and a short factory-output animation. Current facility perimeters already expand at levels 2 and 3.
4. Inventory hover cards, item descriptions, sell confirmation for rare gear, and drag feedback.
5. A 10-minute boss/event and two alternate wave compositions to extend meaningful play toward 20 minutes.
6. Combat, production, inventory, infection and alert sound sets.

## Failure State

When every living bug is dead, enter Game Over even if eggs, doctors, facilities, or resources remain.

## Remaining Art Polish

- Elite soldier, normal robot and heavy robot animation sheets.
- Doctor walk and cleaning sheet.
- Additional construction/upgrade detail variants per facility (the current perimeter already scales for capacity).
- Factory-output animation, infection overlay, hit sparks, and death dissolve.

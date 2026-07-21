# The Call of a Cave — Colony Loop Specification

## Vision

A science-fiction pixel-art colony game about raising cave bugs in a barren underground settlement. The left side becomes a living camp-city through construction and upgrades; the right side is the invasion front. The player balances food, lifespan, labor, medicine, pollution control, and defense.

## Opening State

- 300 coins.
- 3 juvenile cave bugs in the open nursery.
- 2 eggs in the nursery.
- No adult bugs.
- Factory, barracks, hospital, and medical academy begin as build sites.
- Quicksand and the sand-river hazard are removed from this version.

## Resources

- Juvenile: 120 coins. Appears in the nursery.
- Food: 50 coins per unit. Food is stored in the colony inventory.
- Feeding: select/click a bug to consume one food and restore health.
- Factory metal part: click to collect 200 coins.
- Medical doctor: 300 coins plus 5 food per doctor, with one adult assigned to the academy.

## Bug Life Cycle

One colony year equals 10 real-time seconds. A normal bug lives for 36 years / 360 seconds.

| Stage | Real time | Colony age | Rules |
| --- | ---: | ---: | --- |
| Juvenile | 0–60 s | 0–6 | Wanders; cannot work or train. |
| Adult | 60–300 s | 6–30 | Can be assigned to facilities. |
| Elder | 300–360 s | 30–36 | Uses the elder sprite; dies at 36. |

Every bug has two world-space bars:

- Health: starts at 100 and loses 1 point every 5 seconds.
- Age/lifespan: fills from age 0 to 36, gaining one year every 10 seconds.
- Health 50–100: green; health 20–50: yellow; below 20: red.
- Below 20 health: aging speed ×2; factory efficiency ×0.5.
- On ordinary or soldier hit: flash red, shake briefly, and receive light knockback.

### Reproduction and Role Trade-off

- Only unassigned normal adults lay eggs.
- A soldier, medical doctor, factory worker, barracks trainee, hospital patient, or academy assignee does not lay eggs.
- Baseline laying interval: 120 seconds, with the first egg randomized between 70 and 120 seconds so several adults do not lay simultaneously.
- Eggs hatch after 30 seconds; the live-creature cap remains 40 for the first balance pass.
- A free adult therefore produces roughly 1–2 eggs during its 240-second adult stage. Assigning it to production or a profession sacrifices that future population growth.

## Disease and Pollution

- An elder dying leaves a mutated oil-like contamination source.
- A soldier dying in combat also leaves contamination.
- A bug touching contamination becomes infected.
- Infection causes health loss ×2 and aging speed ×2.
- Multipliers stack with critical-health aging.
- An infected bug placed in a hospital is cured after 10 seconds.
- Infection penalties continue during treatment.
- The player must drag the cured bug out of the hospital.

## Facilities

Normal wandering bugs treat every constructed facility except the nursery as a wall and path around it. Adults enter only when the player drags and drops them into a valid facility.

The nursery is only a spawn point. Every unassigned juvenile, adult, elder, and soldier can wander across the entire map without player orders. As the right side becomes dangerous, keeping valuable bugs in safe facilities or manually relocating them is a central risk-management mechanic.

### Factory

- Build: 100 coins.
- Level 1 / 2 / 3 capacity: 3 / 5 / 10.
- Upgrade costs: 600, then 1000 coins.
- Assigned adults are permanently committed and cannot be dragged out.
- Assignment changes the adult to the dedicated factory-worker appearance.
- Each healthy adult produces one metal part every 10 seconds.
- Critical-health adults produce at half efficiency.
- A produced part appears beside the factory and grants 200 coins when clicked.

### Barracks

- Build: 200 coins.
- Level 1 / 2 / 3 capacity: 3 / 5 / 10.
- Upgrade costs: 300, then 500 coins.
- Adult soldier training consumes two colony years / 20 seconds.
- Soldiers have walk, attack, and hit states plus a combat-health bar.
- Soldier maximum remaining lifespan is half that of a normal bug.
- Aging speed during combat: ×1.5.

### Hospital

- Build: 500 coins.
- Level 1 / 2 / 3 capacity: 3 / 5 / 10.
- Upgrade costs: 300, then 500 coins.
- Infected bugs are cured after 10 seconds inside.
- Injured soldiers also recover combat health there.
- Patients remain assigned until manually removed.

### Medical Academy

- Build: 1000 coins.
- Level 1 / 2 / 3 batch size: 2 / 4 / 6 doctors.
- Upgrade costs: 2000, then 3000 coins.
- A batch requires one assigned adult; every doctor in that batch costs 300 coins and 5 food.
- Training time: 20 seconds.
- A doctor can be dragged by the player.
- An idle doctor automatically seeks nearby contamination.
- Cleaning takes 2 seconds and displays a progress bar.
- After cleaning, the doctor automatically returns to the academy; manual return is faster.

## Invasion and Combat

- The first invasion starts after 180 seconds from game start.
- Enemies enter from the right side.
- Initial enemy type: a small scavenger robot.
- Robot attack and defense are slightly stronger than one soldier.
- Two soldiers should reliably defeat one robot; one soldier may win but is likely to die.
- Soldiers automatically engage nearby enemies after the player deploys them.
- Combat-health reaching zero kills a soldier immediately and leaves contamination.

## Failure State

When every living bug is dead, enter Game Over even if eggs, doctors, or facilities remain. The failure screen, toast, alert, and transition block remain bilingual UI systems.

## Interaction Rules

- Nursery has no wall and can be crossed freely.
- All unassigned bugs wander across the complete cave, not only near the nursery or on the colony side.
- Unbuilt facility site: click to construct if resources allow.
- Constructed facility: Shift-click to upgrade.
- Medical academy: normal click starts a doctor batch when its requirements are met.
- Adult bugs are assigned by drag-and-drop.
- Factory workers are locked in; hospital patients and trained roles remain draggable where specified.
- All player-facing strings must be added to `LocalizationTable` in both Chinese and English.

## Implementation Roadmap

1. Remove all sand-river scene objects, scripts, notifications, and text references.
2. Lock the opening state, resource prices, health decay, and 36-year lifecycle.
3. Add world-space health/age/combat bars and the elder visual.
4. Add buildable/upgradable facility sites and obstacle avoidance.
5. Implement factory worker commitment, timed parts, and click collection.
6. Implement infection, pollution, hospital treatment, and combat healing.
7. Implement barracks training, soldiers, robot invasion, and combat.
8. Implement academy batches, doctors, automatic cleaning, and return behavior.
9. Finish bilingual UI feedback, visual states per facility level, balancing, and play-mode tests.

## Required Art Set

- Existing: egg, juvenile, adult idle/walk, factory worker, soldier idle/walk/attack, barren cave background, facility rings.
- Added in this pass: elder bug, scavenger robot, mutated-oil contamination, medical doctor.
- Still desirable for final polish: robot walk/attack sheet, doctor walk/clean sheet, metal-part pickup animation, three construction/upgrade variants for each facility, food icon, infection overlay, hit sparks, and death dissolve.

# Project Planetio DEMO

## Overview

Planetio is a mobile PvE sci-fi base-defense game. You drive a modular vehicle, drop onto planets and defend your base from waves of enemies. Between levels you upgrade your vehicle and base with materials and blueprints, then move on to harder levels.

The **demo** is a solo project made to experiment and understand how the systems work together. It will not be published. It should be a functional game with a clear start and end, with nothing special beyond that.

## Technical

- **Engine:** Unity
- **Platform:** Android first, iOS maybe later
- **Orientation:** Landscape (lots of action on screen)
- **Team:** Everything is made by one person for now
- **Perspective:** 3D, camera diagonal, above the vehicle
- **Art style:** Realistic environment with a cartoon look

## Plot

### Story

Earth is abandoned. A small group of mechanics lives on a space station and travels to different planets, where they research new technology and materials to survive. You are the driver of the vehicle and the chief of the station, responsible for the crew's survival.

### Levels

- A few planets at the beginning of the project, each with 4-5 levels and its own specific materials.
- Each planet has different enemies, and every level is harder than the last.
- Every level has **3 waves** of enemies, each stronger than the previous one.
- The last wave of the last level has a **final boss**. Beating it gives you control of the planet and a passive material income.

## Gameplay

### Controls

- **Two virtual sticks:** one moves the vehicle, the other aims the turret. Movement and aim are independent.
- **Auto-aim option:** the turret can aim at the nearest target automatically.
- **Automatic firing:** the turret fires whenever a target is inside its **firing cone**. The cone depends on the turret (for example, a sniper has a tight, long cone, while a shotgun has a wide, short one).
- **Camera:** follows the vehicle.
- **Abilities:** after the demo.

### Round rules

- Enemies spawn at fixed points, such as a landing ship or a cave.
- Enemies head for the base. They attack whatever is closest, including your vehicle.
- If the vehicle is destroyed there is **no respawn**. A special attribute or ability might allow it later.
- **Losing a round** (vehicle or base destroyed, you retreat): you keep **half of the loot** you collected and lose all **blueprints**. Then repair your base and try again.
- **Breaks:** each break lasts around 10 or 30 seconds and can be skipped. The base opens and restores some of the vehicle's health, or you can simply take a real-life break.

### Map

- Bigger than one screen, but not massive, so camera movement is needed.
- Obstacles and hard terrain: some terrain slows you down, some makes you slide.
- Later (after the demo): some weapons can destroy obstacles or shoot through them.

## Your vehicle

You can modify both the hull and the turret. The hull defines the movement, and each option has pros and cons.

### Hulls

- `Basic hull (wheels):` moves like a car, all stats are medium (speed, turn, armor, health).
- `Tank hull (tracks):` bigger hull, slow movement, can turn while stationary, more health and armor.
- `Flying hull (thrusters):` smaller hull, great acceleration, turns like sliding on ice so it is easy to dodge, but low armor and health.

### Turrets

- `Cannon:` single target, moderate damage and fire rate.
- `Laser:` single target, damage over time, high precision, **high armor penetration**.
- `Machine gun:` can hit multiple targets because of the high bullet spread, high fire rate, low damage.
- `Shotgun:` multiple targets, bullets spread and lose damage over distance, high damage at close range.
- `Flamethrower:` multiple targets, starts with very low damage that grows the longer it keeps hitting. Sets targets on fire (damage over time) even after it stops attacking them. **High armor penetration.**

Each turret has its own firing cone (see Controls).

## Base

Every soul needs a cozy place, so you and your vehicle won't go solo on a foreign planet. The base repairs your vehicle during the level, stores the loot from enemies and offers help during the level. You have to protect it, otherwise you must retreat, repair the base and try again.

- **Storage:** limited space that you upgrade over time.
- **Vehicle section:** your own space. You can upgrade it to give special help, such as repairing your vehicle, boosting fire rate and damage, adding storage with a better loot collector so you get more loot, or an AI drone that helps defeat enemies.

## Enemies

Basic enemies are robots and aliens, in specific categories:

- `Detonator:` small, very low HP, high speed. It only deals damage when it reaches you, where it explodes and dies.
- `Warrior:` melee, above-average HP and armor, slow. It has to get close and hit you.
- `Thrower:` moderate range, throws projectiles that deal moderate damage, slow fire rate, low health and armor.

### Combat rules

- **Armor** reduces damage by a **percentage**.
- Turrets with high armor penetration (laser, flamethrower) are strong against tanky enemies.
- Each wave is stronger than the previous one.
- Each planet can have different enemy skins and some improved stats.
- All stat numbers (hulls, turrets, enemies) will be tuned through experimenting.

### Drops

Enemies drop materials and blueprints depending on their type. Tanky enemies drop blueprints for HP and armor, fast enemies drop blueprints for speed, and so on.

## Upgrades and progression

- Hulls, turrets and the base can be upgraded. The demo has **3 upgrade tiers** for each.
- An upgrade needs **materials** (found on different planets) and a **blueprint** (a random enemy drop).
- **One blueprint drop** is enough to unlock an upgrade.
- **Duplicate blueprints** turn into planet materials, or the materials needed to craft that blueprint item.
- **Passive income:** once a planet is taken over, it gives a small amount of its materials every hour.

## Economy and monetization (planned, after the demo)

- **Ads:** none during gameplay or at the end of a round. The player can optionally watch an ad to double the round rewards.
- **Home screen ad rewards:** watch an ad for random materials, or to claim a planet's passive rewards immediately instead of waiting.
- **Monthly subscriptions:** one gives double rewards without watching ads, another gives a small amount of gems daily.
- **Gems:** the only currency in the game, shown as a galaxy crypto-chip coin. Gems buy blueprints in a daily shop/market, or rare materials.

## Demo scope

- **1 planet, 3 levels,** with 3 waves per level and a **final boss** in the last wave of level 3.
- **All 3 hulls** and **all 5 turrets**.
- **3 enemy types:** detonator, warrior, thrower.
- **3 upgrade tiers** for hulls, turrets and the base.
- Base with storage and vehicle section.
- Loot, blueprints and the upgrade system.
- Start and end of the game, so the demo can be played through.

## After the demo

- Vehicle abilities.
- Vehicle skin and color customization.
- Attributes that boost vehicle stats.
- More planets and more enemy variety, including planet-specific skins and stats.
- Bosses with unique abilities.
- Destructible obstacles and shooting through obstacles.
- Vehicle respawn as a special attribute or ability.
- Monetization: ads, subscriptions, gems and the daily shop.
- Replayability: star ratings per level, a harder mode, an endless mode or daily challenges.
- Planet hazards such as acid pools, storms or low visibility.
- In-level pickups (repair kits, damage boosts) and crew members who unlock base upgrades.
- Tutorial and extra game feel polish (screen shake, damage numbers, hit effects, haptics, sound).
- iOS version.

# CF Character Builder + combat (MVP)

## Character builder (lobby)

- Overlay UI lives under `Content.Client/_CF/Lobby/UI/` (see `lobby-overlay.md`).
- Profile field: `HumanoidCharacterProfile.CFBuild` (`CFCharacterBuildData`) — background, class, heritage, ancestry free boosts, background choice/free boosts, four finishing free boosts, background/class trained skill picks.
- Ancestry is `Profile.Species` resolved through `cfAncestry` prototypes. Player Core ancestries are Human, Dwarf, Elf, Gnome, Goblin, Halfling, Leshy, and Orc. Player Core 2 adds the uncommon ancestries Catfolk, Hobgoblin, Kholo, Kobold, Lizardfolk, Ratfolk, Tengu, and Tripkee. Lost Omens Ancestry Guide adds Azarketi, Fetchling, and Kitsune (uncommon) and Android, Fleshwarp, Sprite, and Strix (rare). Every ancestry that is not stock Human or Dwarf uses the humanoid sprite (`CFMob*`). Stock and `_CE` species without a `cfAncestry` have `roundStart: false`, so they leave the ancestry list.
- Heritage is `CFBuild.Heritage` (`cfHeritage`). The Level 1 Heritage button opens the same Common / Uncommon / Rare / All list as ancestry. The list is that ancestry's heritages plus versatile heritages (empty `ancestries`). Changing ancestry clears a heritage that no longer fits. Hit Points, Speed, size, and darkvision on the heritage replace the ancestry values. Other heritage abilities are a one-line note in the detail pane. Aasimar, tiefling, and aphorite are Nephilim.
- Sprite is another species id on the same ancestry (`cfAncestry.sprites`). The Level 1 Sprite button uses the same list. The ancestry's own species stays the humanoid body. Extra bodies are `roundStart: false` and still count for `FindAncestry`. `HumanoidCharacterProfile.EnsureValid` keeps them because of that; a `roundStart: false` species with no ancestry is still reset to Human. Picking a different ancestry resets the sprite to that ancestry's humanoid body. Bodies whose art uses `chest_m` and `groin_m` draw the pelvis on `HumanoidVisualLayers.Groin`, behind the chest. Imported art stays in the fork texture folder it came from (`Resources/Textures/_Starlight`, `_Impstation`, `_DV`, `_EinsteinEngines`, `_Goobstation`, `_CE`), including `meta.json`. Do not copy it under `_CF`. The sprite picker appends that parts RSI's license and copyright. The path is `cfAncestry.sprites.parts`. The humanoid option reads `Mobs/Species/Human/parts.rsi`. Kobold can use the ancestor sheet `Mobs/Animals/Ancestors/kobold.rsi` (one full-body state on the chest) or a reptilian body under its own species id. `_CE` Infernal is Crystall Edge's tiefling sheet after they renamed the folder. It is a sprite on every ancestry with `heritage: Nephilim`, so the picker shows it only while that heritage is selected, and each ancestry has its own species id. Horns and the tail are required markings. Skeleton stays textures only. Stock Vox, Reptilian, Diona, and Vulpkanin RSIs stay in `Mobs/Species`. Clothing offset uses the stock `Inventory.displacements` maps in `displacements.yml`, pointed at that species' `displacement.rsi`. Markings use `markingsDisplacement` on the same maps. Ears, snouts, and tails drawn for that body are `canBeDisplaced: false` and live in the fork `Customization` folder. Do not port a fork's displacement shader.
- Traits are `cfTrait` prototypes under `Resources/Prototypes/_CF/Traits/`, one file per category. Kingdom traits are not included. The remaster dropped good, evil, lawful, and chaotic, so `alignment.yml` is holy, unholy, and sanctified. Spell schools were removed; `school.yml` only keeps the old names so a stray reference still resolves. Spawned objects and creatures carry `CFTraitComponent`. Spells store the same ids on `cfSpell.traits`.
- Attribute math is PF2e modifiers (+0 base, boost +1, flaw −1) via `CFAttributeMath`.
- Level 1 buttons: **Abilities** (`BoostAssignmentView`), **Skill** / **Class** (`SkillAssignmentView`). Within one step (ancestry, background, or the four free boosts; likewise each skill screen), a picked attribute or skill is removed from the other rows. Ancestry free boosts also omit that ancestry's fixed boosts and flaws. Accept stays disabled until every row has a remaining choice, so a duplicate cannot be saved and then reappear as blank.
- Skill Training: one slot per background `trainedSkills`/`skillBoosts`; player assigns distinct skills (defaults from background).
- Backgrounds: `Resources/Prototypes/_CF/Character/Background/`. Player Core and the other Archives of Nethys backgrounds. Each restricted attribute boost is listed (the builder always adds one free boost). A Lore subcategory is the `Lore` skill; the specific lore is named in the description. Skill feats are named only; they are not granted.
- Ancestry feats are imported through level 5. Files are one feat each under `Resources/Prototypes/_CF/Character/Feats/Ancestry/<Ancestry>/` (`cfFeat`). The import is remaster books under the ORC license (Player Core, Player Core 2, and later ORC books). Android, Azarketi, and Fleshwarp have no files: their feats are still the pre-remaster Ancestry Guide. The Level 1 Ancestry Feat button lists feats for the current ancestry at the character's level or lower. Choosing one trains its skills. A Lore name is shown on the Lore row. A land Speed bonus is added at spawn. An activity, extra attack, or granted feat the game cannot perform yet is marked in the description with `TBA - action will be added`.
- Class feats are imported through level 5 under `Resources/Prototypes/_CF/Character/Feats/Class/<Class>/`, one file per feat. A feat several classes can take is copied into each of those class folders. The Level 1 Class Feat button lists that class's feats of level 1. The Level 2 button lists feats of level 2 or lower, excluding the level 1 pick. Skills, lores, and land Speed are applied. Anything else says `This feature will be implemented later.` Level 4 feats are in the folders and are not on a button yet.
- Classes are one `cfClass` file each under `Resources/Prototypes/_CF/Character/Class/Core/` (Player Core) and `Class/Expanded/` (Player Core 2 and later ORC remaster books). The chassis is level 1. Features through level 5 are listed on the class entry; a feature the game does not run says `This feature will be implemented later.` Bard, Fighter, Alchemist, and Barbarian use the remaster ranks, armor, and fixed skills. Kineticist is not included.
- Class Skill: `additionalSkillTrainings + max(0, Int)` free picks; excludes fixed class + background skills.
- Sheet preview (HP / AC / saves) uses `CFAttributeMath.Snapshot`.
- Spells tab (`SpellsTab.xaml`) lists slots by rank. A class with `spellcasting` uses its cantrips and slot counts (Bard: 5 cantrips, two rank-1 spells). Other classes show a preview sheet (5 cantrips, 3/2/2). Slot buttons use the standard button style. `SpellSelectionPopup.xaml` filters `cfSpell` prototypes by tradition and rank. Choices are stored on `CFCharacterBuildData.Spells`. Skill feats and spell descriptions are short mechanical notes.

## Spawn

- `Content.Server/_CF/Character/CFCharacterSpawnSystem` listens to `PlayerSpawnCompleteEvent` and applies:
  - `CFAbilityScoreComponent` (modifiers)
  - `CFCombatantComponent` (level, AC, HP, class/background ids, trained skill picks)
  - Movement scaled from ancestry Speed (25 feet is the usual human pace). Darkvision adds night vision.

## In-game character sheet

- Overlay: `Content.Client/_CF/UserInterface/Systems/Character/` (same csproj Remove pattern as lobby — stock `CharacterWindow` / `CharacterUIController` stay on disk).
- Sheet UI: `Content.Client/_CF/Character/CFCharacterSheetTab` — abilities, AC/HP/saves, 3-column skills. Roll targets are white `Label`s (hand cursor, `UIClick`), same pattern as crystall-edge `DndSheetSkillRow`. A Font Awesome d20 (`CFDiceLabel`, U+F6CF, `Resources/Fonts/_CF/FontAwesome/fa-solid-900.ttf`) sits beside each ability, save, and skill name. Click the die, the name, or an ability modifier to emote a d20 check.
- Skill/save math: `CFSkills` + class/background proficiency lists. Ability “scores” shown as `10 + 2×mod` (CF stores modifiers).

## Combat (lean PF2e Strike)

- `CFMeleeWeapon` on weapons + `CFCombatant` + `CFAbilityScore` on attackers/targets.
- `CFStrikeSystem` handles `MeleeHitEvent` on CF weapons: cancels stock damage, rolls d20+mod vs AC, applies CF HP. Marking the hit handled skips the stock sounds. A melee hit plays `soundHit`. A melee miss plays `soundSwing` at the target. A CF weapon with no `soundHit` uses a slash or smash sound from its damage type. A swing that touches nothing still uses the stock swing sound.
- Floating numbers (`CFDamageNumberSystem` / `CFCombatNumberEvent`): **HIT / MISS / CRIT HIT / CRIT MISS**, then damage amount on hits.
- Entities:
  - `CFLongsword` — d8 slashing martial weapon
  - `CFLeatherArmor` — +1 AC, Dex cap 3, LightArmor
  - `CFTrainingDummy` — AC 15, 20 HP (`spawn CFTrainingDummy`)
- Vanilla targets (`CFCombatBridgeSystem`):
  - **Mobs** (have `MobState`): treated as PF2e **Commoner** (−1, AC 13, HP 10) on first Strike.
  - **Objects**: inferred `cfMaterial` (hardness/HP) from `damageModifierSet` / `damageContainer`, or set `CFMaterial` explicitly. Hardness subtracted from damage; 0 HP breaks/destroys via Destructible.
  - CF HP is the source of truth. `DamageableSystem.SetDamage` rewrites the SS14 bar as Blunt from missing CF HP, kept under the Critical threshold. Dying sets that bar to Critical; death sets it to Dead.
- **Unarmed**: empty hands (body or gloves `MeleeWeapon`) use PF2e fist Strike — 1d4 bludgeoning, `Unarmed` proficiency — instead of stock SS14 blunt.
- **Ranged**: `CFRangedWeapon` on bows; on projectile collide (`BeforeProjectileHitEvent`) resolve Dex Strike (range increments, volley −2, deadly die). `CFLongbow` — 1d8 P, range 100 ft, deadly d10, volley 30 ft (wield to shoot, nock `CFArrow`).
- **Dying (PF2e)**: at 0 HP → SS14 `Critical` + dying 1 (2 on crit knockout) + wounded. Recovery flat check every 6s (DC 10 + dying). Dying 4 → `Dead`. Strikes on downed raise dying (+2 on crit). Alt-click **Stabilize (Medicine)** (DC 5 + dying). Stabilize revives at 1 HP and clears bleed; `RestoreHitPoints` heals CF HP + `HealEvenly` across SS14 damage types. Stock rejuvenate restores CF HP to max and clears dying, then flanking is checked again.
- **Alchemy / potions**: `CFAlchemicalItem` + optional `CFHealConsumable` under `Content.Shared/_CF/Alchemy/`. PF2e traits as Tags (`CFConsumable`, `CFHealing`, `CFMagical`, `CFPotion`, `CFVitality`, …) in `Resources/Prototypes/_CF/tags.yml`. `CFHealingPotionMinor` — 1d8 HP (`spawn CFHealingPotionMinor`).
- **Flanking / off-guard**: `CFFlankingSystem`. Each combatant gets a non-solid reach sensor (`hard: false`, radius `CFFlanking.ReachInUnits`). Physics reports when that circle overlaps another body. `PreventCollideEvent` drops the contact unless the other body is a mob and hostile (`CFAlliance.Opposition` when both have a CF faction; otherwise not allies). Allies do not keep a sensor contact. When two allies both overlap the same foe, the line between their centers must pass within `CFFlanking.BodyRadius` (0.25) of the foe. The foe gains `CFOffGuardComponent`: −2 circumstance AC against those flankers' melee Strikes only. While that player is off-guard, the HUD shield keeps their normal AC struck through and draws the reduced AC in red. Dead and dying creatures never receive it. The line test runs for overlapped foes when someone moves or a contact starts or ends, not for every body on the map. The `CFFlankedIcon` status icon is drawn on the right. While debugging, `CFOffGuardLabelSystem` also draws green `OffGuard` text above any creature with the condition active.
- **Factions**: `CFFactionSystem` and `cfFaction` prototypes in `Resources/Prototypes/_CF/Factions/factions.yml`. Alliances are Party, Opposition, and Neutral. Same-id members use the prototype `same` field (adventure companies are Party; Neutral's `same` is Neutral). A Party target is dropped before the strike: no AC, no d20, no damage. Melee cancels in `GettingAttackedAttemptEvent`; projectiles zero damage and return in `CFStrikeSystem.OnBeforeProjectileHit`. Opposition can damage and is added to NPC hostile queries. Neutral is not hunted and does not hunt other neutrals, but can still be struck. Jobs map on spawn: `CFAlpha`/`CFBeta`/`CFCharlie`/`CFDelta` to those factions; merchant and arcane jobs to Neutral.

## Loadouts (Weapons / Defense / Gear)

- Prototypes: `Resources/Prototypes/_CF/Loadouts/cf_loadouts.yml`
- `roleLoadout` ids are `JobCFAlpha` … (must match `Job` + job id).
- Groups use `editorTab: Weapons|Defense|Gear`. Those tabs list only what was bought. The Shop tab (after Details) buys and sells them at full price.
- Starting coin: level 0 is 5 gp, level 1 and up is 15 gp. Stored as copper and shown as pp / gp / sp / cp, rolling up at every 10. Nothing is selected for free (`minLimit` and `defaultSelected` are 0).
- In-game `CFPurseComponent` is that leftover coin. The character-menu shop spends it at full price. A 50% merchant sell rate is not implemented.

## Dev / Sandbox spawn checklist

`cfJobGroup` only fills the **character builder** job list. Round join needs:

1. Job on the map's `StationJobs.availableJobs` — Dev is patched in `Resources/Prototypes/Maps/debug.yml`.
2. A real `department` listing those jobs — `Resources/Prototypes/_CF/Roles/departments.yml` (late-join GUI).
3. Development preset: `Resources/ConfigPresets/Build/development.toml` must keep `ic.random_characters = false` or spawn replaces your profile (and wipes `CFBuild`).
4. Set High priority on a CF job (e.g. Alpha), save, Ready / Join.

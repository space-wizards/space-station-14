# Solid material fires

Hot items gradually ignite nearby exposed solid fuel. Holding a hot item and using it on fuel starts a repeating interaction; movement, damage, changing hands, extinguishing the tool, wetting the target, or losing oxygen interrupts it. Items inside inventories and containers do not passively ignite things outside them.

## Starting balance

| Material | Cigarette | Lighter | Torch | Burnout after ignition |
| --- | ---: | ---: | ---: | ---: |
| Towels, cloth, cloth uniforms, cardboard | 90 s | 9 s | 3 s | 60 s |
| Carpet items, placed carpets, carpet tiles | 240 s | 24 s | 8 s | 90 s |
| Wood planks, wooden furniture, walls and floors | 300 s | 30 s | 10 s | 120 s |

Times assume uninterrupted dry contact, sufficient oxygen, and default settings. The server samples passive contact approximately once per second. Manual use also advances in one-second interactions. The strongest source applies; piling up cigarettes does not multiply the heating rate. Removing a source cools accumulated exposure at two cigarette-equivalent seconds per second.

Burning entities spread heat at rate 20: a nearby towel therefore takes about five seconds to catch. Burnout destroys the fuel entity and creates one Ash entity, without running normal damage-based material recovery. A material stack is one fuel entity. Floors expose the previous floor layer using normal tile history, without producing a reusable floor tile. Extinguishing pauses fuel consumption; already burned time is retained.

Coverage is explicit through prototype inheritance. It includes cloth uniforms, all towel variants, cloth/cardboard/wood material stacks, carpet items and placed carpets, wooden chairs/tables/walls, wooden floor items, and wood/carpet floor tile definitions. Other objects can opt in using the profiles below; hard armor and unrelated objects are not automatically classified by their names.

## Live server controls

These are server CVars and can be changed without rebuilding. For example:

```text
cvar fire.solid_fuel_enabled false
cvar fire.solid_fuel_spread false
cvar fire.solid_fuel_ignition_multiplier 0.5
```

The first command prevents new solid fuel fires, cancels ignition interactions, clears pending heating, and extinguishes existing solid fuel fires on the next one-second update. Existing atmospheric gas fire and mob fire systems retain their normal behavior. Re-enabling starts contact heating from zero.

| CVar | Default | Purpose |
| --- | ---: | --- |
| `fire.solid_fuel_enabled` | `true` | Emergency switch for this system |
| `fire.solid_fuel_spread` | `true` | Allow burning entities to passively heat nearby solid fuel |
| `fire.solid_fuel_ignition_multiplier` | `1` | Contact heating multiplier; 0.5 doubles ignition time, 0 stops contact heating |
| `fire.solid_fuel_burn_multiplier` | `1` | Fuel consumption multiplier; 0.5 doubles burnout time, 0 freezes consumption |
| `fire.solid_fuel_contact_range` | `0.6` | Passive range of hot tools and cigarettes, in tiles |
| `fire.solid_fuel_spread_range` | `1.1` | Passive range of burning entities, in tiles |
| `fire.solid_fuel_fire_rate` | `20` | Heating rate of burning entities |
| `fire.solid_fuel_cigarette_rate` | `1` | Heating rate of lit smokables |

Ranges are clamped to 0–3 tiles; zero disables that passive range. Wall obstruction checks still apply. A floor directly under a source counts as contact across its surface. Rate multipliers are clamped to non-negative values. These controls apply to the new contact system; pre-existing gas-fire, chemical and weapon ignition retain their normal routes, with wetness/oxygen/enabled checks for solid fuel.

Persistent server configuration example:

```toml
[fire]
solid_fuel_enabled = true
solid_fuel_spread = false
solid_fuel_ignition_multiplier = 0.5
solid_fuel_burn_multiplier = 1.0
```

## Per-material and per-source tuning

`Resources/Prototypes/Entities/Objects/Misc/solid_fuel.yml` defines three reusable parent profiles: `BaseSolidFuel`, `BaseWoodFuel`, and `BaseCarpetFuel`. Change their values to rebalance all inheriting objects, or override one object's component:

```yaml
- type: SolidFuel
  ignitionTime: 180 # cigarette-equivalent seconds
  burnTime: 120     # seconds actually spent burning
  coolingRate: 3    # exposure lost each second without contact
  ashPrototype: Ash
```

Keep ignition and burn times positive and cooling rates non-negative. The profile also supplies `Flammable`, `Reactive`, appearance and fire visuals. Avoid adding ordinary damage-based salvage triggers to burnout.

Each `IgnitionSource` has `contactIgnitionRate`, default 10. Torches explicitly use 30. This is separate from its atmospheric hotspot temperature, so balancing carpet ignition need not change gas reactions. Lit smokables use the cigarette CVar; extinguished and spent smokables contribute no heat. Burning entities use the fire-rate CVar.

Tile prototypes opt in with `solidFuelEntity: SolidFuelFloorWood` or `SolidFuelFloorCarpet`. The system creates temporary floor fuel only when a heat source reaches a combustible tile, and removes it if that tile is replaced. It does not create fuel entities for every floor on the map. Floor fuel stores the tile prototype ID, and existing entities are located directly instead of using a runtime-only cache, so loaded fires keep their accumulated burn progress.

## Extinguishing and wetness

Water and fire extinguishers use the existing `Extinguish` reagent reaction. Tile spray reactions also reach non-colliding carpets and floor fires even without an atmospheric hotspot. Negative fire stacks block ignition and dry using the existing flammable-system rules. Solid fuel also retains a serialized wetness timer so incendiary weapons cannot erase recent wetness by adding positive stacks. The timer adds one second per negative stack applied, capped at ten seconds, matching the normal stack limit. Collidable objects receive the existing entity reaction; the additional tile reaction is limited to non-colliding fuel. Extinguishing clears accumulated heat.

Absorbed extinguishing liquids in towels and extinguishing puddles under objects also block ignition. Reagents are recognized through their extinguishing effect definitions, rather than a hard-coded list of water item names. An absorbed solution stays wet until removed; this feature does not add a separate towel evaporation model.

Starting and sustaining fire requires the existing one-mole oxygen minimum. Airtight wooden walls use oxygen at their exposed neighboring faces. Losing oxygen extinguishes the fire. No separate gas combustion chemistry or smoke-generation model is added.

## Validation

Run the targeted integration tests with:

```text
dotnet test Content.IntegrationTests --no-build --filter "FullyQualifiedName~SolidFuelTest"
```

Before deploying on a populated server, playtest a dropped cigarette on a towel and carpet, a held lighter, water spray on a burning carpet, oxygen removal, adjacent carpet spread, and the emergency switch. The automated tests verify behavior; the starting values still need gameplay balance feedback.

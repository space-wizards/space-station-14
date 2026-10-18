# Genetics

Genetics lets medical staff scan a creature, discover its latent mutations by solving nucleotide sequences, and apply them with injectors. Active mutations change the subject and add instability.

## Overview

| Part | Location |
|------|----------|
| Mutation prototypes | `Resources/Prototypes/Genetics/mutations_{positive,neutral,negative}.yml` |
| Action prototypes | `Resources/Prototypes/Actions/genetics.yml` |
| Injector entities | `Resources/Prototypes/Entities/Objects/Specific/Genetics/injectors.yml` |
| Mutadone reagent | `Resources/Prototypes/Reagents/medicine_genetics.yml` |
| Localization | `Resources/Locale/en-US/genetics/genetics-console.ftl` |
| Guidebook | `Resources/ServerInfo/Guidebook/Medical/Genetics.xml` |

## Code structure

| Class | Side | Responsibility |
|-------|------|----------------|
| `GeneticMutationPrototype` | Shared | Data definition of a mutation: id, category, sequence, instability and effect fields. |
| `GeneticMutationComponent` | Shared | Per-creature genome: active mutations, state flags for reversible effects, timers, granted actions. |
| `SharedGeneticsSystem` | Shared | Applies and removes mutations and raises `GeneticMutationAppliedEvent` / `GeneticMutationRemovedEvent`. |
| `GeneticActionEvents` | Shared | Events fired by genetic actions (telepathy, telekinesis, shock touch, laser eyes, species shift). |
| `GeneticsConsoleSystem` | Server | Console logic: scanning, sequence solving, storing mutations, crafting injectors. |
| `GeneticMutationEffectsSystem` | Server | Gameplay effects of every mutation. Reacts to the applied/removed events and adds or reverts the effect. |
| `GeneticInstabilitySystem` | Server | Sums instability of active mutations and deals cellular damage above the threshold. |
| `GeneticMutationInjectorSystem` | Server | Injecting a stored mutation into a target. |
| `GeneticsConsoleBoundUserInterface`, `GeneticsConsoleWindow` | Client | Console UI. |

## Lifecycle of a mutation

1. The console scans a subject and rolls up to seven potential mutations, each with its own sequence.
2. The player solves the sequence by pairing nucleotides (A with T, G with C). A correct sequence discovers the mutation.
3. A discovered mutation can be stored in the console and written to a `GeneticMutationInjector`.
4. Injecting adds the id to `GeneticMutationComponent.ActiveMutations` and raises `GeneticMutationAppliedEvent`.
5. `GeneticMutationEffectsSystem.ApplyMutationEffects` reads the prototype fields and applies the effects.
6. Removal (for example by Mutadone) raises `GeneticMutationRemovedEvent`, and `RemoveMutationEffects` reverts everything that was added.

## Instability

Each mutation has an `instability` value. The total of all active mutations is the subject's instability. Above 100 the subject takes 15 cellular damage every 10 seconds, and at 150 cellular damage they die. Negative values reduce the total.

## Adding a mutation

1. Add a `geneticMutation` prototype to the matching YAML file with `id`, `sequence`, `instability` and the effect fields it needs.
2. Add `genetics-mutation-<name>-name` and `genetics-mutation-<name>-description` to the `.ftl` file.
3. If no existing prototype field covers the effect, add a field to `GeneticMutationPrototype`, a state flag to `GeneticMutationComponent` if the effect must be reverted, and handle both in `ApplyMutationEffects` and `RemoveMutationEffects`.
4. If the mutation grants an action, add an action prototype and event, and register the handler in `GeneticMutationEffectsSystem.Initialize`.

Effects must be fully reversible: a removed mutation has to leave the creature as it was before.

## Notes for contributors

- Systems with `[Dependency]` fields must be `partial`.
- Action prototype ids passed to `AddAction` must be `EntProtoId` constants, not string literals.
- Run `Content.YAMLLinter` after changing prototypes.

## Mutation list

Instability is shown as defined in the prototypes.

### Positive

| Id | Instability |
|----|-------------|
| GeneticMutationHulk | 55 |
| GeneticMutationXRay | 50 |
| GeneticMutationTelekinesis | 45 |
| GeneticMutationRegeneration | 40 |
| GeneticMutationIronLungs | 40 |
| GeneticMutationTelepathy | 35 |
| GeneticMutationNobleGas | 35 |
| GeneticMutationLaserEyes | 35 |
| GeneticMutationFireResistance | 30 |
| GeneticMutationColdResistance | 30 |
| GeneticMutationRadiationResistance | 30 |
| GeneticMutationAdaptiveVision | 25 |
| GeneticMutationCryokinesis | 25 |
| GeneticMutationInsulated | 25 |
| GeneticMutationMindreader | 25 |
| GeneticMutationTemperatureAdaptation | 25 |
| GeneticMutationThermalVision | 25 |
| GeneticMutationThickSkin | 20 |
| GeneticMutationAntenna | 10 |
| GeneticMutationGeladikinesis | 10 |
| GeneticMutationOlfaction | 10 |
| GeneticMutationPressureAdaptation | 10 |
| GeneticMutationShockTouch | 10 |
| GeneticMutationWebbing | 10 |
| GeneticMutationClever | 10 |
| GeneticMutationVoid | 5 |

### Neutral

| Id | Instability |
|----|-------------|
| GeneticMutationChameleon | 25 |
| GeneticMutationSpeciesShift | 20 |
| GeneticMutationSizeSmall | 15 |
| GeneticMutationSizeLarge | 15 |
| GeneticMutationBioluminescence | 10 |
| GeneticMutationAnimalHearing | 10 |
| GeneticMutationSnail | 5 |
| GeneticMutationPigLatin | 0 |
| GeneticMutationWacky | 0 |
| GeneticMutationChav | -10 |
| GeneticMutationElvis | -10 |
| GeneticMutationSwedish | -10 |
| GeneticMutationAntiGlow | -10 |

### Negative

| Id | Instability |
|----|-------------|
| GeneticMutationNarcolepsy | -40 |
| GeneticMutationEpilepsy | -30 |
| GeneticMutationTourette | -30 |
| GeneticMutationParanoia | -30 |
| GeneticMutationClumsiness | -30 |
| GeneticMutationGlassJaw | -20 |
| GeneticMutationNoises | -20 |
| GeneticMutationCough | -30 |
| GeneticMutationMute | -30 |
| GeneticMutationIlliteracy | -30 |
| GeneticMutationNearsighted | -30 |
| GeneticMutationRadioactive | -30 |
| GeneticMutationSpastic | -30 |
| GeneticMutationSpatialInstability | -30 |
| GeneticMutationStuttering | -30 |
| GeneticMutationTwoLeftFeet | -30 |
| GeneticMutationAcidFlesh | -40 |
| GeneticMutationBlindness | -40 |
| GeneticMutationDeafness | -40 |
| GeneticMutationUnintelligible | -40 |

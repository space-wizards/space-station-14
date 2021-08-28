namespace Content.IntegrationTests.Tests.Destructible
{
    public static class DestructibleTestPrototypes
    {
        public const string SpawnedEntityId = "DestructibleTestsSpawnedEntity";
        public const string DestructibleEntityId = "DestructibleTestsDestructibleEntity";
        public const string DestructibleDestructionEntityId = "DestructibleTestsDestructibleDestructionEntity";
        public const string DestructibleDamageTypeEntityId = "DestructibleTestsDestructibleDamageTypeEntity";
        public const string DestructibleDamageGroupEntityId = "DestructibleTestsDestructibleDamageGroupEntity";

        public static readonly string Prototypes = $@"
- type: damageType
  id: TestBlunt

- type: damageType
  id: TestSlash

- type: damageType
  id: TestPiercing

- type: damageType
  id: TestHeat

- type: damageType
  id: TestShock

- type: damageType
  id: TestCold

- type: damageType
  id: TestPoison

- type: damageType
  id: TestRadiation

- type: damageType
  id: TestAsphyxiation

- type: damageType
  id: TestBloodloss

- type: damageType
  id: TestCellular

- type: damageGroup
  id: TestBrute
  damageTypes:
    - TestBlunt
    - TestSlash
    - TestPiercing

- type: damageGroup
  id: TestBurn
  damageTypes:
    - TestHeat
    - TestShock
    - TestCold

- type: damageGroup
  id: TestAirloss
  damageTypes:
    - TestAsphyxiation
    - TestBloodloss

- type: damageGroup
  id: TestToxin
  damageTypes:
    - TestPoison
    - TestRadiation

- type: damageGroup
  id: TestGenetic
  damageTypes:
    - TestCellular

- type: damageContainer
  id: TestAllDamageContainer
  supportAll: true


- type: damageContainer
  id: TestBiologicalDamageContainer
  supportedGroups:
    - TestBrute
    - TestBurn
    - TestToxin
    - TestAirloss
    - TestGenetic

- type: damageContainer
  id: TestMetallicDamageContainer
  supportedGroups:
    - TestBrute
    - TestBurn

- type: entity
  id: {SpawnedEntityId}
  name: {SpawnedEntityId}

- type: entity
  id: {DestructibleEntityId}
  name: {DestructibleEntityId}
  components:
  - type: Damageable
    damageContainer: TestMetallicDamageContainer
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 20
        triggersOnce: false
    - trigger:
        !type:DamageTrigger
        damage: 50
        triggersOnce: false
      behaviors:
      - !type:PlaySoundBehavior
        sound:
            path: /Audio/Effects/woodhit.ogg
      - !type:SpawnEntitiesBehavior
        spawn:
          {SpawnedEntityId}:
            min: 1
            max: 1
      - !type:DoActsBehavior
        acts: [""Breakage""]
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDestructionEntityId}
  name: {DestructibleDestructionEntityId}
  components:
  - type: Damageable
    damageContainer: TestMetallicDamageContainer
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 50
      behaviors:
      - !type:PlaySoundBehavior
        sound:
            path: /Audio/Effects/woodhit.ogg
      - !type:SpawnEntitiesBehavior
        spawn:
          {SpawnedEntityId}:
            min: 1
            max: 1
      - !type:DoActsBehavior # This must come last as it destroys the entity.
        acts: [""Destruction""]
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDamageTypeEntityId}
  name: {DestructibleDamageTypeEntityId}
  components:
  - type: Damageable
    damageContainer: TestMetallicDamageContainer
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageTypeTrigger
          damageType: TestBlunt
          damage: 10
        - !type:DamageTypeTrigger
          damageType: TestSlash
          damage: 10
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDamageGroupEntityId}
  name: {DestructibleDamageGroupEntityId}
  components:
  - type: Damageable
    damageContainer: TestMetallicDamageContainer
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageGroupTrigger
          damageGroup: TestBrute
          damage: 10
        - !type:DamageGroupTrigger
          damageGroup: TestBurn
          damage: 10
  - type: TestThresholdListener";
    }
}

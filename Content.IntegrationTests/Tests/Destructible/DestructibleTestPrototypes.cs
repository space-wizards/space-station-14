namespace Content.IntegrationTests.Tests.Destructible
{
    public static class DestructibleTestPrototypes
    {
        public const string SpawnedEntityId = "DestructibleTestsSpawnedEntity";
        public const string DestructibleEntityId = "DestructibleTestsDestructibleEntity";
        public const string DestructibleDestructionEntityId = "DestructibleTestsDestructibleDestructionEntity";
        public const string DestructibleDamageTypeEntityId = "DestructibleTestsDestructibleDamageTypeEntity";
        public const string DestructibleDamageGroupEntityId = "DestructibleTestsDestructibleDamageGroupEntity";
        public const string TestBruteDamageGroupId = "TestBrute";
        public const string TestBurnDamageGroupId = "TestBurn";
        public const string TestBluntDamageTypeId = "TestBlunt";
        public const string TestSlashDamageTypeId = "TestSlash";
        public const string TestPiercingDamageTypeId = "TestPiercing";
        public const string TestHeatDamageTypeId = "TestHeat";
        public const string TestShockDamageTypeId = "TestShock";
        public const string TestColdDamageTypeId = "TestCold";

        [TestPrototypes]
        public const string DamagePrototypes = $@"
- type: damageType
  id: {TestBluntDamageTypeId}
  name: damage-type-blunt

- type: damageType
  id: {TestSlashDamageTypeId}
  name: damage-type-slash

- type: damageType
  id: {TestPiercingDamageTypeId}
  name: damage-type-piercing

- type: damageType
  id: {TestHeatDamageTypeId}
  name: damage-type-heat

- type: damageType
  id: {TestShockDamageTypeId}
  name: damage-type-shock

- type: damageType
  id: {TestColdDamageTypeId}
  name: damage-type-cold

- type: damageGroup
  id: {TestBruteDamageGroupId}
  name: damage-group-brute
  damageTypes:
    - {TestBluntDamageTypeId}
    - {TestSlashDamageTypeId}
    - {TestPiercingDamageTypeId}

- type: damageGroup
  id: {TestBurnDamageGroupId}
  name: damage-group-burn
  damageTypes:
    - {TestHeatDamageTypeId}
    - {TestShockDamageTypeId}
    - {TestColdDamageTypeId}

- type: entity
  id: {SpawnedEntityId}
  name: {SpawnedEntityId}

- type: entity
  id: {DestructibleEntityId}
  name: {DestructibleEntityId}
  components:
  - type: Damageable
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
            collection: WoodDestroy
      - !type:SpawnEntitiesBehavior
        spawn:
          {SpawnedEntityId}:
            min: 1
            max: 1
      - !type:DoActsBehavior
        acts: [""Breakage""]

- type: entity
  id: {DestructibleDestructionEntityId}
  name: {DestructibleDestructionEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 50
      behaviors:
      - !type:PlaySoundBehavior
        sound:
            collection: WoodDestroyHeavy
      - !type:SpawnEntitiesBehavior
        spawn:
          {SpawnedEntityId}:
            min: 1
            max: 1
      - !type:DoActsBehavior # This must come last as it destroys the entity.
        acts: [""Destruction""]

- type: entity
  id: {DestructibleDamageTypeEntityId}
  name: {DestructibleDamageTypeEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageTypeTrigger
          damageType: {TestBluntDamageTypeId}
          damage: 10
        - !type:DamageTypeTrigger
          damageType: {TestSlashDamageTypeId}
          damage: 10

- type: entity
  id: {DestructibleDamageGroupEntityId}
  name: {DestructibleDamageGroupEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageGroupTrigger
          damageGroup: {TestBruteDamageGroupId}
          damage: 10
        - !type:DamageGroupTrigger
          damageGroup: {TestBurnDamageGroupId}
          damage: 10";
    }
}

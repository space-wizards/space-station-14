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
        sound: /Audio/Effects/woodhit.ogg
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
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 50
      behaviors:
      - !type:PlaySoundBehavior
        sound: /Audio/Effects/woodhit.ogg
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
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageTypeTrigger
          damageType: Blunt
          damage: 10
        - !type:DamageTypeTrigger
          damageType: Slash
          damage: 10
  - type: TestThresholdListener

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
          damageGroup: Brute
          damage: 10
        - !type:DamageGroupTrigger
          damageGroup: Burn
          damage: 10
  - type: TestThresholdListener";
    }
}

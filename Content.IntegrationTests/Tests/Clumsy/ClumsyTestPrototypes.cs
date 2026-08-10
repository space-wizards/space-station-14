using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Clumsy;

public static class ClumsyTestPrototypes
{
    public static readonly EntProtoId TargetProto = "MobHuman";
    public static readonly EntProtoId GunProto = "WeaponPistolMk58";
    public static readonly EntProtoId SyringeProto = "Syringe";

    public const string ClumsyStatusAll100 = "ClumsyStatusAll100";
    public const string BallProto = "BallProto";
    public const string DefibProto = "DefibProto";
    public const string TableProto = "TableProto";

    [TestPrototypes]
    public static readonly string ClumsyPrototypes = @$"
- type: entity
  id: {ClumsyStatusAll100}
  components:
  - type: StatusEffect
  - type: ClumsyCatchStatusEffect
    clumsyChance: 1
  - type: ClumsyDefibStatusEffect
    clumsyChance: 1
  - type: ClumsyGunStatusEffect
    clumsyChance: 1
  - type: ClumsyInjectorStatusEffect
    clumsyChance: 1
  - type: ClumsyVaultStatusEffect
    clumsyChance: 1

- type: entity
  id: {BallProto}
  components:
  - type: Item
  - type: Catchable

- type: entity
  id: {DefibProto}
  components:
  - type: Item
  - type: Defibrillator
    zapHeal:
      types:
        Brute: 0

- type: entity
  id: {TableProto}
  components:
  - type: Climbable
  - type: Bonkable
";
}

using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.StatusEffectNew;

public static class StatusEffectNewTestPrototypes
{

    public static readonly EntProtoId TargetProto = "MobHuman";

    public const string StatusA = "StatusA";
    public const string StatusB = "StatusB";
    public const string StatusC = "StatusC";
    
    public static readonly TimeSpan OneSecond = new TimeSpan(0, 0, 0, 1);
    public static readonly TimeSpan OneMinute = new TimeSpan(0, 0, 1, 0);
    public static readonly TimeSpan TenTicks = new TimeSpan(10L);

    [TestPrototypes]
    public static readonly string StatusEffectPrototypes = @$"
- type: entity
  id: {StatusA}
  components:
  - type: StatusEffect

- type: entity
  id: {StatusB}
  components:
  - type: StatusEffect

- type: entity
  id: {StatusC}
  components:
  - type: StatusEffect
";
}
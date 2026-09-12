using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.StatusEffectNew;

public static class StatusEffectNewTestPrototypes
{

    public static readonly EntProtoId TargetProto = "MobHuman";

    public const string StatusA = "StatusA";
    public const string StatusB = "StatusB";
    public const string StatusC = "StatusC";
    public const string StatusD = "StatusD";
    
    public static readonly long One = 1L;
    public static readonly TimeSpan OneTick = new TimeSpan(One);
    public static readonly long Ten = One * 10;
    public static readonly TimeSpan TenTicks = new TimeSpan(Ten);

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

- type: entity
  id: {StatusD}
  components:
  - type: StatusEffect
";
}
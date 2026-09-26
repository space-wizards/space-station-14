using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.StatusEffectNew;

public static class StatusEffectNewTestPrototypes
{
    public const string StatusA = "StatusA";
    public const string StatusB = "StatusB";
    public const string StatusC = "StatusC";

    public static readonly TimeSpan TenTicks = new TimeSpan(10);

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
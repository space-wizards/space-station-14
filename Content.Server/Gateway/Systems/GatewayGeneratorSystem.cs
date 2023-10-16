using Content.Server.Salvage;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Gateway.Systems;

/// <summary>
/// Generates gateway destinations regularly and indefinitely that can be chosen from.
/// </summary>
public sealed class GatewayGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GatewayGeneratorComponent, MapInitEvent>(OnGeneratorMapInit);
    }

    private void OnGeneratorMapInit(EntityUid uid, GatewayGeneratorComponent component, MapInitEvent args)
    {
        for (var i = 0; i < 3; i++)
        {
            component.Seeds.Add(_random.Next());
        }

        Dirty(uid, component);
    }

    public void TakeOption(EntityUid uid, GatewayGeneratorComponent component, int index)
    {
        if (component.Seeds.Count >= index)
        {
            Log.Error($"Tried to take invalid seed option {index} for {ToPrettyString(uid)}");
            return;
        }

        var seed = component.Seeds[index];
        component.Seeds.RemoveAt(index);
        component.TakenSeeds.Add(seed);
    }
}

/// <summary>
/// Generates gateway destinations at a regular interval.
/// </summary>
[RegisterComponent]
public sealed partial class GatewayGeneratorComponent : Component
{
    /// <summary>
    /// Next time another seed unlocks.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextUnlock;

    /// <summary>
    /// How long it takes to unlock another destination once one is taken.
    /// </summary>
    [DataField]
    public TimeSpan UnlockCooldown = TimeSpan.FromMinutes(45);

    [DataField]
    public List<int> Seeds = new();

    [DataField]
    public List<int> TakenSeeds = new();
}

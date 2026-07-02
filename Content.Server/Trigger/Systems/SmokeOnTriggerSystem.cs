using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;

using Content.Shared.Maps;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;


namespace Content.Server.Trigger.Systems;

/// <summary>
/// Handles creating smoke when <see cref="SmokeOnTriggerComponent"/> is triggered.
/// </summary>
public sealed partial class SmokeOnTriggerSystem : EntitySystem
{

    [Dependency] private MapSystem _map = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SpreaderSystem _spreader = default!;
    [Dependency] private TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<SmokeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (!_smoke.TrySpawnSmoke(target, ent.Comp.SmokePrototype, out var smoke))
            return;
        _smoke.StartSmoke(smoke.Value, ent.Comp.Solution, (float)ent.Comp.Duration.TotalSeconds, ent.Comp.SpreadAmount, smoke.Value.Comp);

        args.Handled = true;
    }
}

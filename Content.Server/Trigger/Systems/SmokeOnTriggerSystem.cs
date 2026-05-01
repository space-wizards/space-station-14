using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;

using Content.Shared.Maps;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;


namespace Content.Server.Trigger.Systems;

/// <summary>
/// Handles creating smoke when <see cref="SmokeOnTriggerComponent"/> is triggered.
/// </summary>
public sealed class SmokeOnTriggerSystem : EntitySystem
{

    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;

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

        if (!_smoke.SpawnSmoke(target, ent.Comp.SmokePrototype, out var smoke, out var smokeComp))
            return;
        _smoke.StartSmoke(smoke.Value, ent.Comp.Solution, (float)ent.Comp.Duration.TotalSeconds, ent.Comp.SpreadAmount, smokeComp);

        args.Handled = true;
    }
}

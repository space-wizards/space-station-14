using Content.Server.Atmos.EntitySystems;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// Trigger system for setting something on fire.
/// </summary>
/// <seealso cref="IgniteOnTriggerSystem"/>
public sealed class FlameStackOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flame = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlameStackOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<FlameStackOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _flame.AdjustFireStacks(target.Value, ent.Comp.FireStacks, ignite: ent.Comp.DoIgnite);

        args.Handled = true;
    }
}

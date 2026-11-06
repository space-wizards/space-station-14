using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnRadialMenuSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnRadialMenuComponent, TriggerOnRadialMenuSelectMessage>(OnRadialMenuSelect);
    }

    private void OnRadialMenuSelect(Entity<TriggerOnRadialMenuComponent> entity, ref TriggerOnRadialMenuSelectMessage args)
    {
        if (args.Index < 0 || args.Index >= entity.Comp.RadialMenuEntries.Count)
        {
            Log.Error($"{ToPrettyString(args.Actor)} tried to select radial menu index that is out of range for {ToPrettyString(entity)}.");
            return;
        }

        var selected = entity.Comp.RadialMenuEntries[args.Index];
        _trigger.Trigger(entity, args.Actor, selected.Key);
    }
}

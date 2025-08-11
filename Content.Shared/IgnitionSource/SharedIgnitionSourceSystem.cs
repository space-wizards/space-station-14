using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Temperature;

namespace Content.Shared.IgnitionSource;

/// <summary>
/// Ignites flammable gases when the ignition source is toggled on.
/// Also makes the entity hot so that it can be used to ignite matchsticks, cigarettes ect.
/// </summary>
public abstract partial class SharedIgnitionSourceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgnitionSourceComponent, IsHotEvent>(OnIsHot);
        SubscribeLocalEvent<ItemToggleHotComponent, ItemToggledEvent>(OnItemToggle);
        SubscribeLocalEvent<IgnitionSourceComponent, IgnitionEvent>(OnIgnitionEvent);
    }

    private void OnIsHot(Entity<IgnitionSourceComponent> ent, ref IsHotEvent args)
    {
        args.IsHot |= ent.Comp.Ignited;
    }

    private void OnItemToggle(Entity<ItemToggleHotComponent> ent, ref ItemToggledEvent args)
    {
        SetIgnited(ent.Owner, args.Activated);
    }

    private void OnIgnitionEvent(Entity<IgnitionSourceComponent> ent, ref IgnitionEvent args)
    {
        SetIgnited((ent.Owner, ent.Comp), args.Ignite);
    }

    /// <summary>
    /// Simply sets the ignited field to the ignited param.
    /// </summary>
    public void SetIgnited(Entity<IgnitionSourceComponent?> ent, bool ignited = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Ignited = ignited;
        Dirty(ent, ent.Comp);
    }
}

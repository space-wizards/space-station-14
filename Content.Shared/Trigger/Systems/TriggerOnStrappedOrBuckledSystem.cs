using Content.Shared.Buckle.Components;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// This is a system covering all trigger interactions involving strapping or buckling objects.
/// The users of strap components are the objects having an entity strapped to them (IE: Chairs)
/// The users of buckle components are entities being buckled to an object. (IE: Mobs and players)
/// </summary>
public sealed partial class TriggerOnStrappedOrBuckledSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnStrappedComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<TriggerOnUnstrappedComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<TriggerOnBuckledComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<TriggerOnUnbuckledComponent, UnbuckledEvent>(OnUnbuckled);
    }


    #region Class Methods
    // Called by objects entities can be buckled to. (Chairs, surgical tables/)
    private void OnStrapped(Entity<TriggerOnStrappedComponent> ent, ref StrappedEvent args)
    {
        Trigger.Trigger(ent.Owner, args.Strap, ent.Comp.KeyOut);
    }

    private void OnUnstrapped(Entity<TriggerOnUnstrappedComponent> ent, ref UnstrappedEvent args)
    {
        Trigger.Trigger(ent.Owner, args.Strap, ent.Comp.KeyOut);
    }

    // Called by entities that are buckled to an object. (Mobs, players.)
    private void OnBuckled(Entity<TriggerOnBuckledComponent> ent, ref BuckledEvent args)
    {
        Trigger.Trigger(ent.Owner, args.Buckle, ent.Comp.KeyOut);
    }

    private void OnUnbuckled(Entity<TriggerOnUnbuckledComponent> ent, ref UnbuckledEvent args)
    {
        Trigger.Trigger(ent.Owner, args.Buckle, ent.Comp.KeyOut);
    }
    #endregion
}

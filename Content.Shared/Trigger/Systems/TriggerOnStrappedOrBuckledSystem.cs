using Content.Shared.Buckle.Components;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// This is a system covering all trigger interactions involving strapping or unstrapping objects.
/// It is used by several separated components but they all share this same system.
/// This method of implementing the trigger system was requested by a maintainer.
/// </summary>

public sealed partial class TriggerOnStrappedSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnStrappedComponent, StrappedEvent>(OnStrapped);
    }

    private void OnStrapped(Entity<TriggerOnStrappedComponent> ent, ref StrappedEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Strap, ent.Comp.KeyOut);
    }
}

public sealed partial class TriggerOnUnstrappedSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnUnstrappedComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnUnstrapped(Entity<TriggerOnUnstrappedComponent> ent, ref UnstrappedEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Strap, ent.Comp.KeyOut);
    }
}

public sealed partial class TriggerOnBuckledSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnBuckledComponent, BuckledEvent>(OnBuckled);
    }

    private void OnBuckled(Entity<TriggerOnBuckledComponent> ent, ref BuckledEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Buckle, ent.Comp.KeyOut);
    }
}

public sealed partial class TriggerOnUnbuckledSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnUnbuckledComponent, UnbuckledEvent>(OnUnbuckled);
    }

    private void OnUnbuckled(Entity<TriggerOnUnbuckledComponent> ent, ref UnbuckledEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Buckle, ent.Comp.KeyOut);
    }
}

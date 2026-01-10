using Content.Shared.Access.Components;
using Content.Shared.PAI;
using Content.Shared.PDA;
using Robust.Shared.Containers;

namespace Content.Shared.Access.Systems;

public sealed class PAIAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<PAIComponent, EntInsertedIntoContainerMessage>(OnPAIContainerChanged);
        SubscribeLocalEvent<PAIComponent, EntRemovedFromContainerMessage>(OnPAIContainerChanged);
    }

    private void OnGetAdditionalAccess(EntityUid uid, PAIComponent component, ref GetAdditionalAccessEvent args)
    {
        if (_container.TryGetContainingContainer((uid, null, null), out var container))
        {
            args.Entities.Add(container.Owner);
        }
    }

    private void OnPAIContainerChanged(EntityUid uid, PAIComponent component, ContainerModifiedMessage args)
    {
        RaiseLocalEvent(uid, new PAIAccessChangedEvent());
    }
}

public sealed class PAIAccessChangedEvent : EntityEventArgs
{
}

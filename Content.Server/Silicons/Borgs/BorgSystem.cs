using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Mind;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly BodySystem _bobby = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, AfterInteractUsingEvent>(OnChassisInteractUsing);
        SubscribeLocalEvent<BorgChassisComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<BorgChassisComponent, EntRemovedFromContainerMessage>(OnRemoved);

        InitializeMMI();
    }

    private void OnChassisInteractUsing(EntityUid uid, BorgChassisComponent component, AfterInteractUsingEvent args)
    {
        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
            return;

        var used = args.Used;

        if (component.BrainEntity == null &&
            HasComp<BorgBrainComponent>(used) &&
            component.BrainWhitelist?.IsValid(used) != false)
        {
            component.BrainContainer.Insert(used);
        }
    }

    private void OnInserted(EntityUid uid, BorgChassisComponent component, EntInsertedIntoContainerMessage args)
    {
        if (HasComp<BorgBrainComponent>(args.Entity))
        {
            if (_mind.TryGetMind(args.Entity, out var mind))
                _mind.TransferTo(mind, uid);
        }
    }

    private void OnRemoved(EntityUid uid, BorgChassisComponent component, EntRemovedFromContainerMessage args)
    {
        if (HasComp<BorgBrainComponent>(args.Entity))
        {
            if (_mind.TryGetMind(uid, out var mind))
                _mind.TransferTo(mind, args.Entity);
        }
    }
}

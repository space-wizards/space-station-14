using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Labels;

/// <summary>
/// A hand labeler system that lets an object apply labels to objects with the <see cref="LabelComponent"/> .
/// </summary>
[UsedImplicitly]
public sealed class HandLabelerSystem : SharedHandLabelerSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Inform the client of the result of label application
    /// </summary>
    protected override void messageClient(EntityUid targetItem, EntityUid user, LabelAction action)
    {
        RaiseNetworkEvent(
                new HandLabelerMessage(
                    GetNetEntity(targetItem),
                    GetNetEntity(user),
                    (LabelAction) action
                ),
                user);
    }

    protected override void DirtyUI(EntityUid uid, HandLabelerComponent? handLabeler = null)
    {
        if (!Resolve(uid, ref handLabeler))
            return;

        Dirty(uid, handLabeler);
        _userInterfaceSystem.TrySetUiState(uid, HandLabelerUiKey.Key,
            new HandLabelerBoundUserInterfaceState(handLabeler.AssignedLabel));
    }
}

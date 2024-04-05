using Content.Server.UserInterface;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

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

    private void DirtyUI(EntityUid uid,
        HandLabelerComponent? handLabeler = null)
    {
        if (!Resolve(uid, ref handLabeler))
            return;

        _userInterfaceSystem.TrySetUiState(uid, HandLabelerUiKey.Key,
            new HandLabelerBoundUserInterfaceState(handLabeler.AssignedLabel));
    }
}

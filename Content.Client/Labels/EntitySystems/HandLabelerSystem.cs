using Content.Client.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Client.Labels;

public sealed partial class HandLabelerSystem : SharedHandLabelerSystem
{
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    private void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            result = null;
            return;
        }

        if (handLabeler.AssignedLabel == string.Empty)
        {
            _labelSystem.Label(target, null);
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }

        _labelSystem.Label(target, handLabeler.AssignedLabel);
        result = Loc.GetString("hand-labeler-successfully-applied");
    }

}

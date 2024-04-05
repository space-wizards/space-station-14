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
    private readonly Dictionary<int,LabelAction> labeledEntities = new Dictionary<int,LabelAction>();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<HandLabelerMessage>(handleMessages);
    }

    protected override void AddLabelTo(EntityUid uid, HandLabelerComponent? handLabeler, EntityUid target, out string? result, out LabelAction? action)
    {
        if (!Resolve(uid, ref handLabeler))
        {
            action = null;
            result = null;
            return;
        }

        if (handLabeler.AssignedLabel == string.Empty)
        {
            labeledEntities.TryAdd(GetNetEntity(target).Id, LabelAction.Removed);
            action = LabelAction.Removed;
            result = Loc.GetString("hand-labeler-successfully-removed");
            return;
        }

        labeledEntities.TryAdd(GetNetEntity(target).Id, LabelAction.Applied);
        action = LabelAction.Applied;
        result = Loc.GetString("hand-labeler-successfully-applied");
    }

    /// <summary>
    /// Test if our prediction matches with the server,
    /// if it doesn't, create a popup with the valid message.
    /// </summary>
    private void handleMessages(HandLabelerMessage message, EntitySessionEventArgs eventArgs)
    {
        labeledEntities.TryGetValue(message.Target.Id, out var access);
        labeledEntities.Remove(message.Target.Id);
        if(access == message.Action)
            return;

        string result;
        if (message.Action == LabelAction.Removed)
            result = Loc.GetString("hand-labeler-successfully-removed");
        else
            result = Loc.GetString("hand-labeler-successfully-applied");
        _popupSystem.PopupEntity(result, GetEntity(message.User), GetEntity(message.User));
    }

}

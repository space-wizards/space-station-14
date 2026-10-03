using Content.Server.Administration.Systems;
using Content.Shared.Labels.EntitySystems;

namespace Content.Server.Labels.EntitySystems;

public sealed partial class ServerLabelSystem : LabelSystem
{
    [SubscribeLocalEvent]
    private void OnErase(ref EraseEvent args)
    {
        ErasePlayerLabels(args.PlayerNetUserId);
    }
}

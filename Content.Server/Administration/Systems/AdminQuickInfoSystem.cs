using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Server-side of the quick info system.
/// </summary>
/// <remarks>
/// Just a simple "replies to requests from authorized clients".
/// </remarks>
/// <seealso cref="QuickInfoShared" />
public sealed partial class AdminQuickInfoSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<QuickInfoShared.Request>(HandleRequest);
    }

    private void HandleRequest(QuickInfoShared.Request ev, EntitySessionEventArgs args)
    {
        if (!_adminManager.HasAdminFlag(args.SenderSession, QuickInfoShared.RequiredFlag))
        {
            Log.Warning($"{args.SenderSession} tried to fetch entity quick info without required permissions!");
            return;
        }

        var responses = ev.Entities.Select(e =>
            {
                if (!TryGetEntity(e, out var ent))
                    return new QuickInfoShared.SingleEntityInfo(e, false, "", "");

                var metadata = MetaData(ent.Value);
                return new QuickInfoShared.SingleEntityInfo(e, true, metadata.EntityName, metadata.EntityPrototype?.ID);
            })
            .ToArray();

        RaiseNetworkEvent(new QuickInfoShared.Response { Entities = responses }, args.SenderSession);
    }
}

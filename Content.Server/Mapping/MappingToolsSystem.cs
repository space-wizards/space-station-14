using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Content.Shared.Mapping;
using Robust.Server.Player;
using Robust.Shared.Players;

namespace Content.Server.Mapping;

/// <summary>
/// This handles the server side of the mapping tooling with entity spawning/etc.
/// </summary>
public sealed class MappingToolsSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminMan = default!;
    [Dependency] private readonly ILogManager _log = default!;


    private ISawmill _sawmill = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<MappingDrawToolDrawEntityPointEvent>(OnMappingDrawToolDrawPoint);
        _sawmill = _log.GetSawmill("mapping.tools");
    }

    public bool IsMapper(ICommonSession session, bool logOnFail = true)
    {
        if (session is not IPlayerSession realSession)
            return false;

        if (_adminMan.HasAdminFlag(realSession, AdminFlags.Mapping))
            return true;

        _sawmill.Warning($"User {realSession.Name} ({realSession.UserId}) hit an IsMapper check without being a mapper. Verify they've not got into an unusual situation and/or haven't modified their client.");
        return false;
    }

    private void OnMappingDrawToolDrawPoint(MappingDrawToolDrawEntityPointEvent msg, EntitySessionEventArgs args)
    {
        if (!IsMapper(args.SenderSession))
            return;

        var ent = Spawn(msg.Prototype, msg.Point);
        Transform(ent).LocalRotation = msg.Rotation;
    }
}

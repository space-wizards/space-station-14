using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Points;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.Points;

/// <inheritdoc/>
public sealed class PointSystem : SharedPointSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PointManagerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PointManagerComponent, ComponentGetState>(OnGetState);
    }

    private void OnStartup(EntityUid uid, PointManagerComponent component, ComponentStartup args)
    {
        _pvsOverride.AddGlobalOverride(uid);
    }

    private void OnGetState(EntityUid uid, PointManagerComponent component, ref ComponentGetState args)
    {
        args.State = new PointManagerComponentState(component.Points, component.Scoreboard);
    }

    /// <summary>
    /// Adds the specified point value to a player.
    /// </summary>
    [PublicAPI]
    public void AdjustPointValue(EntityUid user, FixedPoint2 value, EntityUid uid, PointManagerComponent? component, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(user, ref actor, false))
            return;
        AdjustPointValue(actor.PlayerSession.UserId, value, uid, component);
    }

    /// <summary>
    /// Sets the amount of points for a player
    /// </summary>
    [PublicAPI]
    public void SetPointValue(EntityUid user, FixedPoint2 value, EntityUid uid, PointManagerComponent? component, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(user, ref actor, false))
            return;
        SetPointValue(actor.PlayerSession.UserId, value, uid, component);
    }

    /// <summary>
    /// Gets the amount of points for a given player
    /// </summary>
    [PublicAPI]
    public FixedPoint2 GetPointValue(EntityUid user, EntityUid uid, PointManagerComponent? component, ActorComponent? actor = null)
    {
        if (!Resolve(uid, ref component) || !Resolve(user, ref actor, false))
            return FixedPoint2.Zero;
        return GetPointValue(actor.PlayerSession.UserId, uid, component);
    }

    /// <inheritdoc/>
    public override FormattedMessage GetScoreboard(EntityUid uid, PointManagerComponent? component = null)
    {
        var msg = new FormattedMessage();

        if (!Resolve(uid, ref component))
            return msg;

        var orderedPlayers = component.Points.OrderByDescending(p => p.Value).ToList();
        var place = 1;
        foreach (var (id, points) in orderedPlayers)
        {
            if (!_player.TryGetPlayerData(id, out var data))
                continue;

            msg.AddMarkup(Loc.GetString("point-scoreboard-list",
                ("place", place),
                ("name", data.UserName),
                ("points", points.Int())));
            msg.PushNewline();
            place++;
        }

        return msg;
    }
}

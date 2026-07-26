using System.Numerics;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Tabletop;

public abstract partial class SharedTabletopSystem
{
    /// <summary>
    ///     Ensures that the <see cref="TabletopGameComponent"/> in the entity passed has a valid session.
    ///     Creates it and sets it up if it doesn't.
    /// </summary>
    /// <param name="tabletop">The tabletop game in question.</param>
    public void EnsureSession(Entity<TabletopGameComponent> ent)
    {
        // We already have a session, return it.
        // TODO: if tables are connected, treat them as a single entity. This can be done by sharing the session.
        if (ent.Comp.HasSession)
            return;

        // We make sure that the tabletop map exists before continuing.
        EnsureTabletopMap();

        // Create new session.
        ent.Comp.Position = new(GetNextTabletopPosition(), TabletopMap);

        // Since this is the first time opening this session, set up the game.
        ent.Comp.Setup.SetupTabletop(ent.Comp, EntityManager);
        Dirty(ent);

        Log.Info($"Created tabletop session number {ent.Comp} at position {ent.Comp.Position}.");
    }

    /// <summary>
    ///     Cleans up a tabletop game session, deleting every entity in it.
    /// </summary>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    public void CleanupSession(EntityUid uid)
    {
        if (!GameQuery.TryComp(uid, out TabletopGameComponent? tabletop))
            return;

        if (!tabletop.HasSession)
            return;

        foreach (var (player, _) in tabletop.Players)
            CloseSessionFor(player, uid);

        foreach (var euid in tabletop.Entities)
            QueueDel(euid);

        tabletop.Players.Clear();
        tabletop.Entities.Clear();
        tabletop.Position = null;
        Dirty(uid, tabletop);
    }

    /// <summary>
    ///     Adds a player to a tabletop game session, sending a message so the tabletop window opens on their end.
    /// </summary>
    /// <param name="player">The player session in question.</param>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    public void OpenSessionFor(ICommonSession player, EntityUid uid)
    {
        if (!GameQuery.TryComp(uid, out TabletopGameComponent? tabletop) || player.AttachedEntity is not { Valid: true } playerUid)
            return;

        // Make sure we have a session, and add the player to it if not added already.
        EnsureSession((uid, tabletop));

        if (_net.IsServer && tabletop.Players.ContainsKey(player))
            return;

        if (TryComp(playerUid, out TabletopGamerComponent? gamer))
            CloseSessionFor(player, gamer.Tabletop, false);

        // Set the entity as an absolute GAMER.
        EnsureComp<TabletopGamerComponent>(playerUid).Tabletop = uid;

        // Create a camera for the gamer to use.
        var camera = CreateCamera(tabletop, player);
        Dirty(uid, tabletop);

        if (_net.IsServer)
        {
            tabletop.Players[player] = new TabletopSessionPlayerData { Camera = camera };
        }

        // Tell the gamer to open a viewport for the tabletop game.
        OnTabletopPlay(uid, camera, Loc.GetString(tabletop.BoardName), tabletop.Size);
    }

    /// <summary>
    /// Removes a player from a tabletop game session, and sends them a message so their tabletop window is closed.
    /// </summary>
    /// <param name="player">The player in question.</param>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    /// <param name="removeGamerComponent">Whether to remove the <see cref="TabletopGamerComponent"/> from the player's attached entity.</param>
    protected void CloseSessionFor(ICommonSession player, EntityUid uid, bool removeGamerComponent = true)
    {
        if (!TryComp(uid, out TabletopGameComponent? tabletop) || !tabletop.HasSession)
            return;

        if (!tabletop.Players.TryGetValue(player, out var data))
            return;

        if (removeGamerComponent && player.AttachedEntity is { } attachedEntity && TryComp(attachedEntity, out TabletopGamerComponent? gamer))
        {
            // We invalidate this to prevent an infinite feedback from removing the component.
            gamer.Tabletop = EntityUid.Invalid;

            // You stop being a gamer.......
            RemComp<TabletopGamerComponent>(attachedEntity);
        }

        tabletop.Players.Remove(player);
        tabletop.Entities.Remove(data.Camera);
        Dirty(uid, tabletop);

        // Deleting the view subscriber automatically cleans up subscriptions, no need to do anything else.
        QueueDel(data.Camera);
    }

    /// <summary>
    ///     A helper method that creates a camera for a specified player, in a tabletop game session.
    /// </summary>
    /// <param name="tabletop">The tabletop game component in question.</param>
    /// <param name="player">The player in question.</param>
    /// <param name="offset">An offset from the tabletop position for the camera. Zero by default.</param>
    /// <returns>The UID of the camera entity.</returns>
    protected EntityUid CreateCamera(TabletopGameComponent tabletop, ICommonSession player, Vector2 offset = default)
    {
        DebugTools.AssertNotNull(tabletop.Position);

        if (tabletop.Position is not { } position)
            return EntityUid.Invalid;

        // Spawn an empty entity at the coordinates.
        var camera = EntityManager.PredictedSpawn(null, position.Offset(offset));

        // Add an eye component and disable FOV.
        var eyeComponent = EnsureComp<EyeComponent>(camera);
        _eye.SetDrawFov(camera, false, eyeComponent);
        _eye.SetZoom(camera, tabletop.CameraZoom, eyeComponent);

        // Add the user to the view subscribers.
        _viewSubscriber.AddViewSubscriber(camera, player);

        return camera;
    }
}

using Content.Shared.Tabletop.Components;
using Robust.Shared.Player;

namespace Content.Shared.Tabletop;

public abstract partial class SharedTabletopSystem
{
    /// <summary>
    /// Ensures that the <see cref="TabletopGameComponent"/> in the entity passed has a valid session.
    /// Creates it and sets it up if it doesn't.
    /// </summary>
    /// <param name="tabletop">The tabletop game in question.</param>
    public void EnsureSession(Entity<TabletopGameComponent> ent)
    {
        // We already have a session, nothing to do.
        if (ent.Comp.HasSession)
            return;

        // We make sure that the tabletop map exists before continuing.
        EnsureTabletopMap();

        // Create new session.
        ent.Comp.Position = new(GetNextTabletopPosition(), TabletopMap);

        // Since this is the first time opening this session, set up the game.
        ent.Comp.Setup.SetupTabletop(ent.Comp, EntityManager);
        Dirty(ent);

        Log.Info($"Created tabletop session for {ent} at position {ent.Comp.Position}.");
    }

    /// <summary>
    /// Cleans up a tabletop game session, deleting every entity in it.
    /// </summary>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    public void CleanupSession(EntityUid uid)
    {
        if (!GameQuery.TryComp(uid, out TabletopGameComponent? tabletop))
            return;

        if (!tabletop.HasSession)
            return;

        foreach (var euid in tabletop.Entities)
            QueueDel(euid);

        tabletop.Entities.Clear();
        tabletop.Position = null;
        Dirty(uid, tabletop);
    }

    /// <summary>
    /// Adds a player to a tabletop game session, sending a message so the tabletop window opens on their end.
    /// </summary>
    /// <param name="player">The player session in question.</param>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    public void OpenSessionFor(ICommonSession player, EntityUid uid)
    {
        if (!GameQuery.TryComp(uid, out TabletopGameComponent? tabletop)
            || player.AttachedEntity is not { Valid: true } playerUid)
            return;

        // Make sure we have a session, and add the player to it if not added already.
        EnsureSession((uid, tabletop));

        // Close the session of the other window we have open.
        if (GamerQuery.TryComp(playerUid, out TabletopGamerComponent? gamer))
            UI.CloseUi(gamer.Tabletop, TabletopGameUiKey.Key, playerUid, true);

        // Set the entity as an ABSOLUTE GAMER.
        EnsureComp<TabletopGamerComponent>(playerUid).Tabletop = uid;
    }

    /// <summary>
    /// Removes a player from a tabletop game session, and sends them a message so their tabletop window is closed.
    /// </summary>
    /// <param name="player">The player in question.</param>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    /// <param name="removeGamerComponent">Whether to remove the <see cref="TabletopGamerComponent"/> from the player's attached entity.</param>
    protected void CloseSessionFor(ICommonSession player, EntityUid uid, bool removeGamerComponent = true)
    {
        if (!GameQuery.TryComp(uid, out TabletopGameComponent? tabletop)
            || !tabletop.HasSession)
            return;

        if (removeGamerComponent && player.AttachedEntity is { } attachedEntity && GamerQuery.TryComp(attachedEntity, out TabletopGamerComponent? gamer))
        {
            // We invalidate this to prevent an infinite feedback from removing the component.
            gamer.Tabletop = EntityUid.Invalid;

            // You stop being a gamer.......
            RemComp<TabletopGamerComponent>(attachedEntity);
        }

        if ()
            if (bui.)
    }
}

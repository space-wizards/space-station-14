using System.Numerics;
using Content.Shared.Tabletop.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.Tabletop;

public abstract partial class SharedTabletopSystem
{
    /// <summary>
    /// Ensures that the <see cref="TabletopGameComponent"/> in the entity passed has a valid session.
    /// Creates it and sets it up if it doesn't.
    /// </summary>
    /// <param name="tabletop">The tabletop game in question.</param>
    public void EnsureBoard(Entity<TabletopGameComponent?> ent)
    {
        // We already have a session, nothing to do.
        if (!Resolve(ent, ref ent.Comp)
            || ent.Comp.HasSession)
            return;

        // We make sure that the tabletop map exists before continuing.
        EnsureTabletopMap();

        // Create new session.
        var position = new MapCoordinates(GetNextTabletopPosition(), TabletopMap);

        // Since this is the first time opening this session, set up the game.
        ent.Comp.Setup.SetupTabletop(ent.Comp, position, EntityManager);

        if (ent.Comp.Board is { } board)
        {
            var coords = new EntityCoordinates(board, Vector2.Zero);

            ent.Comp.UprightCamera = PredictedSpawnAttachedTo(null, coords, rotation: Angle.Zero);
            EnsureComp<EyeComponent>(ent.Comp.UprightCamera.Value);

            ent.Comp.UpsideDownCamera = PredictedSpawnAttachedTo(null, coords, rotation: Angle.FromDegrees(180));
            EnsureComp<EyeComponent>(ent.Comp.UpsideDownCamera.Value);
        }
        Dirty(ent);

        Log.Info($"Created tabletop board for {ent} at position {position}.");
    }

    /// <summary>
    /// Cleans up a tabletop game session, deleting every entity in it.
    /// </summary>
    /// <param name="ent">The tabletop game to tear down.</param>
    public void TeardownBoard(Entity<TabletopGameComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        UI.CloseUis(ent.Owner);

        PredictedQueueDel(ent.Comp.Board);
        // These should be parented to the board, but just in case.
        PredictedQueueDel(ent.Comp.UprightCamera);
        PredictedQueueDel(ent.Comp.UpsideDownCamera);

        ent.Comp.Board = null;
        ent.Comp.UprightCamera = null;
        ent.Comp.UpsideDownCamera = null;

        Dirty(ent);
    }

    /// <summary>
    /// Adds a player to a tabletop game session, sending a message so the tabletop window opens on their end.
    /// </summary>
    /// <param name="player">The player session in question.</param>
    /// <param name="ent">The tabletop game to open a session in.</param>
    public void OpenSessionFor(ICommonSession player, Entity<TabletopGameComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp)
            || player.AttachedEntity is not { Valid: true } playerUid)
            return;

        // Make sure we have a session, and add the player to it if not added already.
        EnsureBoard(ent);

        // Close the session of the other window we have open.
        if (GamerQuery.TryComp(playerUid, out TabletopGamerComponent? gamer))
            UI.CloseUi(gamer.Tabletop, TabletopGameUiKey.Key, playerUid, true);

        // Set the entity as an ABSOLUTE GAMER.
        EnsureComp<TabletopGamerComponent>(playerUid).Tabletop = ent;
    }

    /// <summary>
    /// Removes a player from a tabletop game session, and sends them a message so their tabletop window is closed.
    /// </summary>
    /// <param name="player">The player in question.</param>
    /// <param name="ent">The tabletop game entity.</param>
    /// <param name="removeGamerComponent">Whether to remove the <see cref="TabletopGamerComponent"/> from the player's attached entity.</param>
    protected void CloseSessionFor(ICommonSession player, Entity<TabletopGameComponent?> ent, bool removeGamerComponent = true)
    {
        if (!Resolve(ent, ref ent.Comp)
            || !ent.Comp.HasSession)
            return;

        if (removeGamerComponent && player.AttachedEntity is { } attachedEntity
            && GamerQuery.TryComp(attachedEntity, out TabletopGamerComponent? gamer))
        {
            // We invalidate this to prevent an infinite feedback from removing the component.
            gamer.Tabletop = EntityUid.Invalid;

            // You stop being a gamer.......
            RemComp<TabletopGamerComponent>(attachedEntity);
        }
    }
}

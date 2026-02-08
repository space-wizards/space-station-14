using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Holiday;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Player;

namespace Content.Server.Holiday.Christmas;

/// <summary>
///     This handles handing out items from item givers that only give one item per actual player.
/// </summary>
public sealed class LimitedItemGiverSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedHolidaySystem _holiday = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimitedItemGiverComponent, InteractHandEvent>(OnInteractHand);
    }

    /// <summary>
    ///     Triggers when a player clicks ent. Gives them a gift only once.
    /// </summary>
    private void OnInteractHand(Entity<LimitedItemGiverComponent> ent, ref InteractHandEvent args)
    {
        var (giver, comp) = ent;

        // Santa knows who you are
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        // No present if you've received one
        if (comp.GrantedPlayers.Contains(actor.PlayerSession.UserId))
        {
            if (comp.GreedPopup is { } greedLoc)
                _popup.PopupEntity(Loc.GetString(greedLoc), giver, args.User);

            return;
        }

        var toGive = _entityTable.GetSpawns(comp.Table); // TODO move to shared once this is predicted

        // Get your gifts here
        var success = false;
        foreach (var item in toGive)
        {
            var spawned = SpawnNextToOrDrop(item, args.User);
            _hands.PickupOrDrop(args.User, spawned);
            success = true;
        }

        // Nothing spawned, so don't add the player to the list
        if (!success)
        {
            if (comp.DeniedPopup is { } deniedLoc)
                _popup.PopupEntity(Loc.GetString(deniedLoc), giver, args.User);

            return;
        }

        comp.GrantedPlayers.Add(actor.PlayerSession.UserId);

        if (comp.ReceivedPopup is { } receivedLoc)
            _popup.PopupEntity(Loc.GetString(receivedLoc), giver, args.User);
    }
}

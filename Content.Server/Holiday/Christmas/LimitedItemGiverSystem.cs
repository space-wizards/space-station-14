using Content.Shared.Hands.EntitySystems;
using Content.Shared.Holiday;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Shared.Player;

namespace Content.Server.Holiday.Christmas;

/// <summary>
///     This handles handing out items from item givers that only give one item per actual player.
/// </summary>
public sealed class LimitedItemGiverSystem : EntitySystem
{
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

        // No present if you've received one, or it's not the required holiday (if any)
        if (comp.GrantedPlayers.Contains(actor.PlayerSession.UserId) ||
            comp.RequiredHoliday is { } holiday && !_holiday.IsCurrentlyHoliday(holiday))
        {
            if (comp.DeniedPopup is { } denied)
                _popup.PopupEntity(Loc.GetString(comp.DeniedPopup), giver, args.User);

            return;
        }

        var toGive = EntitySpawnCollection.GetSpawns(comp.SpawnEntries); // TODO move to shared once this is predicted
        var coords = Transform(args.User).Coordinates;

        // Get your gifts here
        foreach (var item in toGive)
        {
            var spawned = SpawnAtPosition(item, coords);
            _hands.PickupOrDrop(args.User, spawned);
        }

        comp.GrantedPlayers.Add(actor.PlayerSession.UserId);

        if (comp.ReceivedPopup is { } received)
            _popup.PopupEntity(Loc.GetString(comp.ReceivedPopup), giver, args.User);
    }
}

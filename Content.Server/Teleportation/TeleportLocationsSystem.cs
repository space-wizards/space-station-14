using Content.Server.Chat.Systems;
using Content.Server.Warps;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Warps;
using Content.Shared.Whitelist;

namespace Content.Server.Teleportation;

/// <summary>
/// <inheritdoc cref="SharedTeleportLocationsSystem"/>
/// </summary>
public sealed partial class TeleportLocationsSystem : SharedTeleportLocationsSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationRequestPointsMessage>(OnTeleportLocationRequest);
    }

    private void OnInit(Entity<TeleportLocationsComponent> ent, ref MapInitEvent args)
    {
        UpdateTeleportPoints(ent);
    }

    private void OnTeleportLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationRequestPointsMessage args)
    {
        UpdateTeleportPoints(ent);
    }

    /// <summary>
    ///     Gets the teleport points to send to the BUI
    /// </summary>
    private void UpdateTeleportPoints(Entity<TeleportLocationsComponent> ent)
    {
        ent.Comp.AvailableWarps.Clear();
        
        var allEnts = AllEntityQuery<WarpPointComponent>();

        while (allEnts.MoveNext(out var warpEnt, out var warpPointComp))
        {
            if (_whitelist.IsBlacklistPass(warpPointComp.Blacklist, warpEnt) || string.IsNullOrWhiteSpace(warpPointComp.Location))
                continue;

            ent.Comp.AvailableWarps.Add(new TeleportPoint(warpPointComp.Location, GetNetEntity(warpEnt)));
        }

        Dirty(ent);
    }

    protected override void OnTeleportToLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        if (!string.IsNullOrWhiteSpace(ent.Comp.Speech))
        {
            var msg = Loc.GetString(ent.Comp.Speech, ("location", args.PointName));
            _chat.TrySendInGameICMessage(args.Actor, msg, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }

        base.OnTeleportToLocationRequest(ent, ref args);
    }
}

using Content.Server.Chat.Systems;
using Content.Server.Warps;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Server.Teleportation;

public sealed partial class TeleportLocationsSystem : SharedTeleportLocationsSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

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

    private void UpdateTeleportPoints(Entity<TeleportLocationsComponent> ent)
    {
        var allEnts = AllEntityQuery<WarpPointComponent>();

        while (allEnts.MoveNext(out var warpEnt, out var warpPointComp))
        {
            if (warpPointComp.GhostOnly || string.IsNullOrWhiteSpace(warpPointComp.Location))
                continue;

            ent.Comp.AvailableWarps.Add(new TeleportPoint(warpPointComp.Location, GetNetEntity(warpEnt)));
        }

        Dirty(ent);
    }

    protected override void OnTeleportLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationRequestTeleportMessage args)
    {
        if (ent.Comp.TeleLocOwner is null)
            return;

        _chat.TrySendInGameICMessage(ent.Comp.TeleLocOwner.Value, $"CHAOS CONTROL ({args.PointName})", InGameICChatType.Speak, ChatTransmitRange.Normal);

        base.OnTeleportLocationRequest(ent, ref args);
    }
}

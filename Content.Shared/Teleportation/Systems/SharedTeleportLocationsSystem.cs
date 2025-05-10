using Content.Shared.Teleportation.Components;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.Warps;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// <inheritdoc cref="TeleportLocationsComponent"/>
/// </summary>
public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    [Dependency] protected readonly UseDelaySystem Delay = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected const string TeleportDelay = "TeleportDelay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportLocationsComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<TeleportLocationsComponent, TeleportLocationDestinationMessage>(OnTeleportToLocationRequest);
    }

    private void OnUiOpenAttempt(Entity<TeleportLocationsComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        args.Cancel();
    }

    protected virtual void OnTeleportToLocationRequest(Entity<TeleportLocationsComponent> ent, ref TeleportLocationDestinationMessage args)
    {
        if (!TryGetEntity(args.NetEnt, out var telePointEnt) || TerminatingOrDeleted(telePointEnt) || !HasComp<WarpPointComponent>(telePointEnt) || Delay.IsDelayed(ent.Owner, TeleportDelay))
            return;

        var comp = ent.Comp;
        var originEnt = args.Actor;
        var telePointXForm = Transform(telePointEnt.Value);

        SpawnAtPosition(comp.TeleportEffect, Transform(originEnt).Coordinates);

        _xform.SetMapCoordinates(originEnt, _xform.GetMapCoordinates(telePointEnt.Value, telePointXForm));

        SpawnAtPosition(comp.TeleportEffect, telePointXForm.Coordinates);

        Delay.TryResetDelay(ent.Owner, true, id: TeleportDelay);

        if (!ent.Comp.CloseAfterTeleport)
            return;

        // Teleport's done, now tell the BUI to close if needed.
        _ui.CloseUi(ent.Owner, TeleportLocationUiKey.Key);
    }
}

using Content.Shared.Teleportation.Components;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.Warps;
using Robust.Shared.Random;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// <inheritdoc cref="TeleportLocationsComponent"/>
/// </summary>
public abstract partial class SharedTeleportLocationsSystem : EntitySystem
{
    [Dependency] protected readonly UseDelaySystem Delay = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected const string TeleportDelay = "TeleportDelay";
    private const int MaxRandomTeleportAttempts = 20;
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

        var coords = telePointXForm.Coordinates;
        var newCoords = coords.Offset(_random.NextVector2(ent.Comp.MaxRandomRadius));

        for (var i = 0; i < MaxRandomTeleportAttempts; i++)
        {
            var randVector = _random.NextVector2(ent.Comp.MaxRandomRadius);
            newCoords = coords.Offset(randVector);
            if (!_lookup.AnyEntitiesIntersecting(_xform.ToMapCoordinates(newCoords), LookupFlags.Static))
            {
                // newCoords is not a wall
                break;
            }
            // after "MaxRandomTeleportAttempts" attempts, end up in the walls
        }

        SpawnAtPosition(comp.TeleportEffect, newCoords);

        _xform.SetMapCoordinates(originEnt, _xform.ToMapCoordinates(newCoords));

        Delay.TryResetDelay(ent.Owner, true, id: TeleportDelay);

        if (!ent.Comp.CloseAfterTeleport)
            return;

        // Teleport's done, now tell the BUI to close if needed.
        _ui.CloseUi(ent.Owner, TeleportLocationUiKey.Key);
    }
}

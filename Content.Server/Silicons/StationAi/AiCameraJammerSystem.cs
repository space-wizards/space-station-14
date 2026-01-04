using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.StationAi.EntitySystems;
using Content.Shared.StationAi;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.StationAi;

public sealed class AiCameraJammerSystem : SharedAiCameraJammerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiCameraJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActiveAiCameraJammerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<ActiveAiCameraJammerComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<ActiveAiCameraJammerComponent, AiCameraJammerPowerLevelChangedEvent>(OnPowerLevelChanged);
    }

    public override void Update(float frameTime)
    {
        // Handle battery drain and throttled position-based updates for held jammers
        var query = EntityQueryEnumerator<ActiveAiCameraJammerComponent, AiCameraJammerComponent, TransformComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var active, out var jammer, out var xform))
        {
            // Try to drain battery
            if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            {
                if (!_battery.TryUseCharge(battery.Value.AsNullable(), GetCurrentWattage((uid, jammer)) * frameTime))
                {
                    // Battery depleted - deactivate
                    DeactivateJammer(uid, active);
                    continue;
                }

                // Update charge level indicator
                var chargeFraction = _battery.GetChargeLevel(battery.Value.AsNullable());
                var chargeLevel = chargeFraction switch
                {
                    > 0.50f => AiCameraJammerChargeLevel.High,
                    < 0.15f => AiCameraJammerChargeLevel.Low,
                    _ => AiCameraJammerChargeLevel.Medium,
                };
                ChangeChargeLevel(uid, chargeLevel);
            }

            // Throttled position checking - only update every 0.1 seconds to reduce spatial query spam
            if (curTime >= active.NextUpdateTime)
            {
                var currentPos = _transform.GetMapCoordinates(uid, xform);

                // Check if position or map changed - dont bother updating if it hasn't moved
                if (active.LastPosition == null ||
                    active.LastPosition.Value.MapId != currentPos.MapId ||
                    !active.LastPosition.Value.Position.EqualsApprox(currentPos.Position, 0.01))
                {
                    UpdateJammedCameras((uid, active, jammer, xform));
                    active.LastPosition = currentPos;
                    Dirty(uid, active);
                }

                // Schedule next update check (0.1 second throttle)
                active.NextUpdateTime = curTime + TimeSpan.FromSeconds(0.1);
            }
        }
    }

    private void OnActivate(Entity<AiCameraJammerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var wasActive = HasComp<ActiveAiCameraJammerComponent>(ent);

        if (wasActive)
        {
            // Was on, turn off
            if (TryComp<ActiveAiCameraJammerComponent>(ent, out var active))
            {
                DeactivateJammer(ent, active);
            }
            var message = Loc.GetString("ai-camera-jammer-on-use",
                ("state", Loc.GetString("ai-camera-jammer-off-state")));
            Popup.PopupEntity(message, args.User, args.User);
        }
        else
        {
            // Was off, try to turn on
            if (_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) &&
                // Make sure battery ain't dead-o
                _battery.GetCharge(battery.Value.AsNullable()) > GetCurrentWattage(ent))
            {
                ChangeLEDState(ent.Owner, true);
                var active = EnsureComp<ActiveAiCameraJammerComponent>(ent);
                active.JammedCameras.Clear();

                // Jam cameras in range (event driven), update our position for offset to get around event driven limitation
                if (TryComp<TransformComponent>(ent, out var xform))
                {
                    UpdateJammedCameras((ent, active, ent.Comp, xform));
                    active.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
                    active.NextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(0.1);
                    Dirty(ent, active);
                }

                var message = Loc.GetString("ai-camera-jammer-on-use",
                    ("state", Loc.GetString("ai-camera-jammer-on-state")));
                Popup.PopupEntity(message, args.User, args.User);
            }
            else
            {
                var message = Loc.GetString("ai-camera-jammer-no-power");
                Popup.PopupEntity(message, args.User, args.User);
            }
        }

        args.Handled = true;
    }

    private void OnParentChanged(Entity<ActiveAiCameraJammerComponent> ent, ref EntParentChangedMessage args)
    {
        // When picked up or dropped, update jammed cameras
        if (TryComp<AiCameraJammerComponent>(ent, out var jammer) &&
            TryComp<TransformComponent>(ent, out var xform))
        {
            UpdateJammedCameras((ent, ent.Comp, jammer, xform));
            ent.Comp.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
            Dirty(ent.Owner, ent.Comp);
        }
    }

    private void OnPowerLevelChanged(Entity<ActiveAiCameraJammerComponent> ent, ref AiCameraJammerPowerLevelChangedEvent args)
    {
        // Power level changed, update jammed cameras with new range
        if (TryComp<AiCameraJammerComponent>(ent, out var jammer) &&
            TryComp<TransformComponent>(ent, out var xform))
        {
            UpdateJammedCameras((ent, ent.Comp, jammer, xform));
            ent.Comp.LastPosition = _transform.GetMapCoordinates(ent.Owner, xform);
            Dirty(ent.Owner, ent.Comp);
        }
    }

    private void OnPowerCellChanged(Entity<ActiveAiCameraJammerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
        {
            DeactivateJammer(ent, ent.Comp);
        }
    }

    private void UpdateJammedCameras(Entity<ActiveAiCameraJammerComponent, AiCameraJammerComponent, TransformComponent> jammer)
    {
        var range = GetCurrentRange((jammer.Owner, jammer.Comp2));
        var jammerPos = _transform.GetMapCoordinates(jammer.Owner, jammer.Comp3);

        // Find all cameras in range
        var camerasInRange = new HashSet<EntityUid>();
        foreach (var camera in _lookup.GetEntitiesInRange<StationAiVisionComponent>(jammerPos, range))
        {
            camerasInRange.Add(camera.Owner);

            // Add jammed marker to camera
            var jammedComp = EnsureComp<AiCameraJammedComponent>(camera);
            jammedComp.JammingSources.Add(jammer.Owner);
            Dirty(camera.Owner, jammedComp);
        }

        // Restore cameras that are no longer in range
        var camerasToRestore = new List<EntityUid>();
        foreach (var previousCamera in jammer.Comp1.JammedCameras)
        {
            if (!camerasInRange.Contains(previousCamera))
            {
                camerasToRestore.Add(previousCamera);
            }
        }

        foreach (var camera in camerasToRestore)
        {
            if (TryComp<AiCameraJammedComponent>(camera, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(jammer.Owner);

                // Only remove component if no other jammers are affecting it
                if (jammedComp.JammingSources.Count == 0)
                {
                    RemComp<AiCameraJammedComponent>(camera);
                }
                else
                {
                    Dirty(camera, jammedComp);
                }
            }
        }

        // Update tracked cameras
        jammer.Comp1.JammedCameras = camerasInRange;
    }

    private void DeactivateJammer(EntityUid uid, ActiveAiCameraJammerComponent active)
    {
        ChangeLEDState(uid, false);

        // Restore all jammed cameras
        foreach (var camera in active.JammedCameras.ToList())
        {
            if (TryComp<AiCameraJammedComponent>(camera, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(uid);

                if (jammedComp.JammingSources.Count == 0)
                {
                    RemComp<AiCameraJammedComponent>(camera);
                }
                else
                {
                    Dirty(camera, jammedComp);
                }
            }
        }

        // Clear the jammed cameras list
        active.JammedCameras.Clear();

        RemComp<ActiveAiCameraJammerComponent>(uid);
    }
}

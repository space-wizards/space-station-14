using System.Linq;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Physics;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDeviceNetworkJammerSystem _jammer = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ActiveRadioJammerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioJammerFixtureComponent, StartCollideEvent>(OnJammerStartCollide);
        SubscribeLocalEvent<RadioJammerFixtureComponent, EndCollideEvent>(OnJammerEndCollide);
        SubscribeLocalEvent<RadioJammerFixtureComponent, ComponentShutdown>(OnFixtureShutdown);
        SubscribeLocalEvent<RadioJammerComponent, RadioJammerPowerLevelChangedEvent>(OnPowerLevelChanged);

        // Event-based battery charging
        SubscribeLocalEvent<ActiveRadioJammerComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<RadioJammerComponent, PowerCellSlotEmptyEvent>(OnPowerCellEmpty);
        SubscribeLocalEvent<ActiveRadioJammerComponent, PredictedBatteryChargeChangedEvent>(OnBatteryChargeChanged);
    }

    private void OnActivate(Entity<RadioJammerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var activated = !HasComp<ActiveRadioJammerComponent>(ent) &&
            _powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) &&
            _battery.GetCharge(battery.Value.AsNullable()) > GetCurrentWattage(ent);
        if (activated)
        {
            ChangeLEDState(ent.Owner, true);
            EnsureComp<ActiveRadioJammerComponent>(ent);
            EnsureComp<DeviceNetworkJammerComponent>(ent, out var jammingComp);
            _jammer.SetRange((ent, jammingComp), GetCurrentRange(ent));
            _jammer.AddJammableNetwork((ent, jammingComp), DeviceNetworkComponent.DeviceNetIdDefaults.Wireless.ToString());

            // Add excluded frequencies using the system method
            if (ent.Comp.FrequenciesExcluded != null)
            {
                foreach (var freq in ent.Comp.FrequenciesExcluded)
                {
                    _jammer.AddExcludedFrequency((ent, jammingComp), (uint)freq);
                }
            }

            // Create collision fixture for camera jamming and range caching
            var fixtureComp = EnsureComp<RadioJammerFixtureComponent>(ent);
            CreateJammerFixture(ent, GetCurrentRange(ent), fixtureComp);

            // Refresh charge rate to start draining battery
            if (_powerCell.TryGetBatteryFromSlot(ent, out var activeBattery))
                _battery.RefreshChargeRate(activeBattery.Value.AsNullable());
        }
        else
        {
            // Destroy collision fixture
            if (TryComp<RadioJammerFixtureComponent>(ent, out var fixtureComp))
            {
                DestroyJammerFixture(ent, fixtureComp);
                RemComp<RadioJammerFixtureComponent>(ent);
            }

            ChangeLEDState(ent.Owner, false);
            RemCompDeferred<ActiveRadioJammerComponent>(ent);
            RemCompDeferred<DeviceNetworkJammerComponent>(ent);

            // Refresh charge rate to stop draining battery
            if (_powerCell.TryGetBatteryFromSlot(ent, out var inactiveBattery))
                _battery.RefreshChargeRate(inactiveBattery.Value.AsNullable());
        }
        var state = Loc.GetString(activated ? "radio-jammer-component-on-state" : "radio-jammer-component-off-state");
        var message = Loc.GetString("radio-jammer-component-on-use", ("state", state));
        Popup.PopupEntity(message, args.User, args.User);
        args.Handled = true;
    }

    private void OnPowerCellChanged(Entity<ActiveRadioJammerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
        {
            // Destroy collision fixture and unjam cameras
            if (TryComp<RadioJammerFixtureComponent>(ent, out var fixtureComp))
            {
                DestroyJammerFixture(ent, fixtureComp);
                RemComp<RadioJammerFixtureComponent>(ent);
            }

            ChangeLEDState(ent.Owner, false);
            RemCompDeferred<ActiveRadioJammerComponent>(ent);
            RemCompDeferred<DeviceNetworkJammerComponent>(ent);
        }
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancelSend(args.RadioSource, args.Channel.Frequency))
        {
            args.Cancelled = true;
        }
    }

    private bool ShouldCancelSend(EntityUid sourceUid, int frequency)
    {
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, RadioJammerFixtureComponent>();

        while (query.MoveNext(out var uid, out _, out var jam, out var fixture))
        {
            // Check if this jammer excludes the frequency
            if (jam.FrequenciesExcluded != null && jam.FrequenciesExcluded.Contains(frequency))
                continue;

            // Use cached collision set instead of range check!
            if (fixture.CollidingEntities.ContainsKey(sourceUid))
                return true;
        }

        return false;
    }

    private void CreateJammerFixture(
        Entity<RadioJammerComponent> ent,
        float range,
        RadioJammerFixtureComponent fixtureComp)
    {
        if (!TryComp<PhysicsComponent>(ent, out var body))
            return;

        var shape = new PhysShapeCircle(range);

        _fixture.TryCreateFixture(
            ent.Owner,
            shape,
            RadioJammerFixtureComponent.FixtureID,
            hard: false,  // Sensor only - no physical collision
            body: body,
            collisionLayer: (int)CollisionGroup.Opaque,
            collisionMask: (int)CollisionGroup.Opaque);

        // Force contact generation
        _physics.RegenerateContacts((ent.Owner, body));
    }

    private void DestroyJammerFixture(
        EntityUid uid,
        RadioJammerFixtureComponent fixtureComp)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        if (fixtures.Fixtures.TryGetValue(RadioJammerFixtureComponent.FixtureID, out var fixture))
        {
            _fixture.DestroyFixture(uid, RadioJammerFixtureComponent.FixtureID, fixture, manager: fixtures);
        }

        // Unjam all cameras
        foreach (var camera in fixtureComp.JammedCameras.ToList())
        {
            if (TryComp<AiCameraJammedComponent>(camera, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(uid);
                if (jammedComp.JammingSources.Count == 0)
                    RemComp<AiCameraJammedComponent>(camera);
                else
                    Dirty(camera, jammedComp);
            }
        }

        fixtureComp.CollidingEntities.Clear();
        fixtureComp.JammedCameras.Clear();
    }

    private void OnJammerStartCollide(
        Entity<RadioJammerFixtureComponent> ent,
        ref StartCollideEvent args)
    {
        if (args.OurFixtureId != RadioJammerFixtureComponent.FixtureID)
            return;

        // Track all colliding entities (for radio range checks)
        ent.Comp.CollidingEntities[args.OtherEntity] = args.OtherBody;

        // If it's a camera, jam it
        if (HasComp<StationAiVisionComponent>(args.OtherEntity))
        {
            ent.Comp.JammedCameras.Add(args.OtherEntity);

            var jammedComp = EnsureComp<AiCameraJammedComponent>(args.OtherEntity);
            jammedComp.JammingSources.Add(ent.Owner);
            Dirty(args.OtherEntity, jammedComp);
        }
    }

    private void OnJammerEndCollide(
        Entity<RadioJammerFixtureComponent> ent,
        ref EndCollideEvent args)
    {
        if (args.OurFixtureId != RadioJammerFixtureComponent.FixtureID)
            return;

        // Remove from collision tracking
        ent.Comp.CollidingEntities.Remove(args.OtherEntity);

        // If it was a jammed camera, unjam it
        if (ent.Comp.JammedCameras.Remove(args.OtherEntity))
        {
            if (TryComp<AiCameraJammedComponent>(args.OtherEntity, out var jammedComp))
            {
                jammedComp.JammingSources.Remove(ent.Owner);

                if (jammedComp.JammingSources.Count == 0)
                    RemComp<AiCameraJammedComponent>(args.OtherEntity);
                else
                    Dirty(args.OtherEntity, jammedComp);
            }
        }
    }

    private void OnFixtureShutdown(Entity<RadioJammerFixtureComponent> ent, ref ComponentShutdown args)
    {
        DestroyJammerFixture(ent.Owner, ent.Comp);
    }

    private void OnPowerLevelChanged(Entity<RadioJammerComponent> ent, ref RadioJammerPowerLevelChangedEvent args)
    {
        // Recreate fixture with new range
        if (TryComp<RadioJammerFixtureComponent>(ent, out var fixtureComp))
        {
            DestroyJammerFixture(ent, fixtureComp);
            CreateJammerFixture(ent, GetCurrentRange(ent), fixtureComp);
        }

        // Update DeviceNetworkJammer range
        if (TryComp<DeviceNetworkJammerComponent>(ent, out var jammingComp))
        {
            _jammer.SetRange((ent, jammingComp), GetCurrentRange(ent));
        }

        // Refresh charge rate since wattage changed
        if (HasComp<ActiveRadioJammerComponent>(ent) &&
            _powerCell.TryGetBatteryFromSlot(ent, out var battery))
        {
            _battery.RefreshChargeRate(battery.Value.AsNullable());
        }
    }

    private void OnRefreshChargeRate(Entity<ActiveRadioJammerComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (!TryComp<RadioJammerComponent>(ent, out var jam))
            return;

        // Set negative charge rate (discharging)
        args.NewChargeRate -= GetCurrentWattage((ent.Owner, jam));
    }

    private void OnPowerCellEmpty(Entity<RadioJammerComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        // Only turn off if it's actually active
        if (!HasComp<ActiveRadioJammerComponent>(ent))
            return;

        // Destroy collision fixture and unjam cameras
        if (TryComp<RadioJammerFixtureComponent>(ent, out var fixtureComp))
        {
            DestroyJammerFixture(ent, fixtureComp);
            RemComp<RadioJammerFixtureComponent>(ent);
        }

        ChangeLEDState(ent, false);
        RemCompDeferred<ActiveRadioJammerComponent>(ent);
        RemCompDeferred<DeviceNetworkJammerComponent>(ent);
    }

    private void OnBatteryChargeChanged(Entity<ActiveRadioJammerComponent> ent, ref PredictedBatteryChargeChangedEvent args)
    {
        // Calculate charge level for LED visuals
        var chargeFraction = args.CurrentCharge / args.MaxCharge;
        var chargeLevel = chargeFraction switch
        {
            > 0.50f => RadioJammerChargeLevel.High,
            < 0.15f => RadioJammerChargeLevel.Low,
            _ => RadioJammerChargeLevel.Medium,
        };
        ChangeChargeLevel(ent, chargeLevel);
    }
}

using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.EnergyDome;

public sealed partial class EnergyDomeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        //Generator events
        SubscribeLocalEvent<EnergyDomeGeneratorComponent, ActivateInWorldEvent>(OnActivatedInWorld);
        SubscribeLocalEvent<EnergyDomeGeneratorComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<EnergyDomeGeneratorComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<EnergyDomeGeneratorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);

        SubscribeLocalEvent<EnergyDomeGeneratorComponent, ComponentRemove>(OnComponentRemove);

        //Dome events
        SubscribeLocalEvent<EnergyDomeComponent, DamageChangedEvent>(OnDomeDamaged);
    }
    private void OnAfterInteract(Entity<EnergyDomeGeneratorComponent> generator, ref AfterInteractEvent args)
    {
        AttemptUse(generator, args.User);
    }

    private void OnActivatedInWorld(Entity<EnergyDomeGeneratorComponent> generator, ref ActivateInWorldEvent args)
    {
        AttemptUse(generator, args.User);
    }

    private void OnPowerCellSlotEmpty(Entity<EnergyDomeGeneratorComponent> generator, ref PowerCellSlotEmptyEvent args)
    {
        TurnOff(generator, true);
    }

    private void OnPowerCellChanged(Entity<EnergyDomeGeneratorComponent> generator, ref PowerCellChangedEvent args)
    {
        if (args.Ejected || !_powerCell.HasDrawCharge(generator))
        {
            TurnOff(generator, false);
        }
    }

    private void OnDomeDamaged(Entity<EnergyDomeComponent> dome, ref DamageChangedEvent args)
    {
        if (dome.Comp.Generator == null)
            return;

        var generatorUid = dome.Comp.Generator.Value;

        if (!TryComp<EnergyDomeGeneratorComponent>(generatorUid, out var generatorComp))
            return;

        if (args.DamageDelta == null)
            return;

        float totalDamage = args.DamageDelta.GetTotal().Float();
        var energyLeak = totalDamage * dome.Comp.EnergyLessForDamage;

        _audio.PlayPvs(dome.Comp.ParrySound, dome);

        if (!_powerCell.TryUseCharge(generatorUid, energyLeak))
        {
            if (_powerCell.TryGetBatteryFromSlot(generatorUid, out var battery))
                _battery.UseCharge(battery.Owner, energyLeak); //Force set Charge to 0%


            TurnOff(new Entity<EnergyDomeGeneratorComponent>(generatorUid, generatorComp), true);
        }
    }

    public void AttemptUse(Entity<EnergyDomeGeneratorComponent> generator, EntityUid user)
    {
        if (TryComp(generator, out LockComponent? lockComp) && lockComp.Locked)
        {
            _audio.PlayPvs(generator.Comp.AccessDeniedSound, generator);
            _popup.PopupEntity(
                Loc.GetString("energy-dome-access-denied"),
                generator,
                user);
            return;
        }

        if (generator.Comp.NextActivation > _gameTiming.CurTime)
        {
            _audio.PlayPvs(generator.Comp.NoPowerSound, generator);
            _popup.PopupEntity(
                Loc.GetString("energy-dome-recharging"),
                generator,
                user);
            return;
        }

        if (!_powerCell.TryGetBatteryFromSlot(generator, out var battery) && !TryComp(generator, out battery))
        {
            _audio.PlayPvs(generator.Comp.NoPowerSound, generator);
            _popup.PopupEntity(
                Loc.GetString("energy-dome-no-cell"),
                generator,
                user);
            return;
        }

        if (battery.Charge < generator.Comp.Wattage)
        {
            _audio.PlayPvs(generator.Comp.NoPowerSound, generator);
            _popup.PopupEntity(
                Loc.GetString("energy-dome-no-power"),
                generator,
                user);
            return;
        }

        Toggle(generator);
    }

    public void Toggle(Entity<EnergyDomeGeneratorComponent> generator)
    {
        if (generator.Comp.Enabled)
        {
            TurnOff(generator, false);
        }
        else
        {
            TurnOn(generator);
        }
    }

    public void TurnOn(Entity<EnergyDomeGeneratorComponent> generator)
    {
        if (generator.Comp.Enabled)
            return;

        generator.Comp.Enabled = true;
        var newDome = Spawn(generator.Comp.DomePrototype, Transform(generator).Coordinates);

        if (TryComp<EnergyDomeComponent>(newDome, out var domeComp))
        {
            domeComp.Generator = generator;
        }

        generator.Comp.SpawnedDome = newDome;
        _audio.PlayPvs(generator.Comp.TurnOnSound, generator);
    }
    public void TurnOff(Entity<EnergyDomeGeneratorComponent> generator, bool startReloading)
    {
        if (!generator.Comp.Enabled)
            return;

        generator.Comp.Enabled = false;
        QueueDel(generator.Comp.SpawnedDome);

        if (startReloading)
        {
            generator.Comp.NextActivation = _gameTiming.CurTime + TimeSpan.FromSeconds(generator.Comp.ReloadSecond);
        }
        _audio.PlayPvs(generator.Comp.TurnOffSound, generator);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EnergyDomeGeneratorComponent>();
        while (query.MoveNext(out var uid, out var generator))
        {
            if (!generator.Enabled)
                continue;

            if (generator.SpawnedDome == null)
                continue;

            if (!_powerCell.TryUseCharge(uid, generator.Wattage * frameTime))
            {
                TurnOff(new Entity<EnergyDomeGeneratorComponent>(uid, generator), true);
                continue;
            };

            _transform.SetCoordinates(generator.SpawnedDome.Value, Transform(uid).Coordinates);
        }
    }
    private void OnComponentRemove(Entity<EnergyDomeGeneratorComponent> generator, ref ComponentRemove args)
    {
        TurnOff(generator, false);
    }
}

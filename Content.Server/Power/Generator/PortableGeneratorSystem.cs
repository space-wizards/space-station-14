using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Power.Generator;
using Content.Shared.Verbs;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Power.Generator;

/// <summary>
/// Implements logic for portable generators (the PACMAN). Primarily UI & power switching behavior.
/// </summary>
/// <seealso cref="PortableGeneratorComponent"/>
public sealed class PortableGeneratorSystem : SharedPortableGeneratorSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GeneratorSystem _generator = default!;
    [Dependency] private readonly PowerSwitchableSystem _switchable = default!;
    [Dependency] private readonly ActiveGeneratorRevvingSystem _revving = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Update UI after main system runs.
        UpdatesAfter.Add(typeof(GeneratorSystem));
        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<PortableGeneratorComponent, GetVerbsEvent<AlternativeVerb>>(GetAlternativeVerb);
        SubscribeLocalEvent<PortableGeneratorComponent, GeneratorStartedEvent>(OnGeneratorStarted);
        SubscribeLocalEvent<PortableGeneratorComponent, AutoGeneratorStartedEvent>(OnAutoGeneratorStarted);
        SubscribeLocalEvent<PortableGeneratorComponent, PortableGeneratorStartMessage>(GeneratorStartMessage);
        SubscribeLocalEvent<PortableGeneratorComponent, PortableGeneratorStopMessage>(GeneratorStopMessage);
        SubscribeLocalEvent<PortableGeneratorComponent, PortableGeneratorSwitchOutputMessage>(GeneratorSwitchOutputMessage);

        SubscribeLocalEvent<FuelGeneratorComponent, SwitchPowerCheckEvent>(OnSwitchPowerCheck);
    }

    private void GeneratorSwitchOutputMessage(EntityUid uid, PortableGeneratorComponent component, PortableGeneratorSwitchOutputMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);
        if (fuelGenerator.On)
            return;

        _switchable.Cycle(uid, args.Session.AttachedEntity.Value);
    }

    private void GeneratorStopMessage(EntityUid uid, PortableGeneratorComponent component, PortableGeneratorStopMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        StopGenerator(uid, component, args.Session.AttachedEntity.Value);
    }

    private void GeneratorStartMessage(EntityUid uid, PortableGeneratorComponent component, PortableGeneratorStartMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        StartGenerator(uid, component, args.Session.AttachedEntity.Value);
    }

    private void StartGenerator(EntityUid uid, PortableGeneratorComponent component, EntityUid user)
    {
        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);
        if (fuelGenerator.On || !Transform(uid).Anchored)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.StartTime, new GeneratorStartedEvent(), uid, uid)
        {
            BreakOnDamage = true, BreakOnMove = true, RequireCanInteract = true,
            NeedHand = true
        });
    }

    private void StopGenerator(EntityUid uid, PortableGeneratorComponent component, EntityUid user)
    {
        _generator.SetFuelGeneratorOn(uid, false);
    }

    private void OnGeneratorStarted(EntityUid uid, PortableGeneratorComponent component, GeneratorStartedEvent args)
    {
        if (args.Cancelled)
            return;

        GeneratorTugged(uid, component, args.User, out args.Repeat);
    }

    private void OnAutoGeneratorStarted(EntityUid uid, PortableGeneratorComponent component, ref AutoGeneratorStartedEvent args)
    {
        GeneratorTugged(uid, component, null, out var repeat);

        // restart the auto rev if it should be repeated
        if (repeat)
            _revving.StartAutoRevving(uid);
        else
            args.Started = true;
    }

    private void GeneratorTugged(EntityUid uid, PortableGeneratorComponent component, EntityUid? user, out bool repeat)
    {
        repeat = false;

        if (!Transform(uid).Anchored)
            return;

        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);

        var empty = _generator.GetFuel(uid) == 0;
        var clogged = _generator.GetIsClogged(uid);

        var sound = empty ? component.StartSoundEmpty : component.StartSound;
        _audio.PlayEntity(sound, Filter.Pvs(uid), uid, true);

        if (!clogged && !empty && _random.Prob(component.StartChance))
        {
            _generator.SetFuelGeneratorOn(uid, true, fuelGenerator);

            if (user is null)
                return;

            _popup.PopupEntity(Loc.GetString("portable-generator-start-success"), uid, user.Value);

        }
        else
        {
            // try again bozo
            repeat = true;

            if (user is null)
                return;

            _popup.PopupEntity(Loc.GetString("portable-generator-start-fail"), uid, user.Value);
        }
    }

    private void GetAlternativeVerb(EntityUid uid, PortableGeneratorComponent component,
        GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);
        if (fuelGenerator.On)
        {
            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StopGenerator(uid, component, args.User);
                },
                Disabled = false,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
                Text = Loc.GetString("portable-generator-verb-stop"),
            };

            args.Verbs.Add(verb);
        }
        else
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var reliable = component.StartChance == 1;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartGenerator(uid, component, args.User);
                },

                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
                Text = Loc.GetString("portable-generator-verb-start"),
            };

            if (!Transform(uid).Anchored)
            {
                verb.Disabled = true;
                verb.Message = Loc.GetString("portable-generator-verb-start-msg-unanchored");
            }
            else
            {
                verb.Message = Loc.GetString(reliable
                    ? "portable-generator-verb-start-msg-reliable"
                    : "portable-generator-verb-start-msg-unreliable");
            }

            args.Verbs.Add(verb);
        }
    }

    private void OnSwitchPowerCheck(EntityUid uid, FuelGeneratorComponent comp, ref SwitchPowerCheckEvent args)
    {
        if (comp.On)
            args.DisableMessage = Loc.GetString("fuel-generator-verb-disable-on");
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PortableGeneratorComponent, FuelGeneratorComponent, PowerSupplierComponent>();

        while (query.MoveNext(out var uid, out var portGen, out var fuelGen, out var powerSupplier))
        {
            UpdateUI(uid, portGen, fuelGen, powerSupplier);
        }
    }

    private void UpdateUI(
        EntityUid uid,
        PortableGeneratorComponent comp,
        FuelGeneratorComponent fuelComp,
        PowerSupplierComponent powerSupplier)
    {
        if (!_uiSystem.IsUiOpen(uid, GeneratorComponentUiKey.Key))
            return;

        var fuel = _generator.GetFuel(uid);
        var clogged = _generator.GetIsClogged(uid);

        (float, float)? networkStats = null;
        if (powerSupplier.Net is { IsConnectedNetwork: true } net)
            networkStats = (net.NetworkNode.LastCombinedLoad, net.NetworkNode.LastCombinedSupply);

        _uiSystem.TrySetUiState(
            uid,
            GeneratorComponentUiKey.Key,
            new PortableGeneratorComponentBuiState(fuelComp, fuel, clogged, networkStats));
    }
}

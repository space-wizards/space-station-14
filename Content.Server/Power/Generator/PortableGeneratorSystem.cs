using Content.Server.DoAfter;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.DoAfter;
using Content.Shared.Power.Generator;
using Content.Shared.Verbs;
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
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GeneratorSystem _generator = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Update UI after main system runs.
        UpdatesAfter.Add(typeof(GeneratorSystem));

        SubscribeLocalEvent<PortableGeneratorComponent, GetVerbsEvent<InteractionVerb>>(GetInteractionVerbs);
        SubscribeLocalEvent<PortableGeneratorComponent, GetVerbsEvent<AlternativeVerb>>(GetAlternativeVerb);
        SubscribeLocalEvent<PortableGeneratorComponent, GeneratorStartedEvent>(GeneratorTugged);
    }

    private void StartGenerator(EntityUid uid, PortableGeneratorComponent component, EntityUid user)
    {
        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);
        if (fuelGenerator.On)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(user, component.StartTime, new GeneratorStartedEvent(), uid, uid)
        {
            BreakOnDamage = true, BreakOnTargetMove = true, BreakOnUserMove = true, RequireCanInteract = true,
            NeedHand = true
        });
    }

    private void StopGenerator(EntityUid uid, PortableGeneratorComponent component, EntityUid user)
    {
        _generator.SetFuelGeneratorOn(uid, false);
    }

    private void GeneratorTugged(EntityUid uid, PortableGeneratorComponent component, GeneratorStartedEvent args)
    {
        if (args.Cancelled)
            return;

        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);

        var empty = fuelGenerator.RemainingFuel == 0;

        var sound = empty ? component.StartSoundEmpty : component.StartSound;
        _audio.Play(sound, Filter.Pvs(uid), uid, true);

        if (!empty && _random.Prob(component.StartChance))
        {
            _popup.PopupEntity(Loc.GetString("portable-generator-start-success"), uid, args.User);
            _generator.SetFuelGeneratorOn(uid, true, fuelGenerator);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("portable-generator-start-fail"), uid, args.User);
            // Try again bozo
            args.Repeat = true;
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
                Message = Loc.GetString(reliable
                    ? "portable-generator-verb-start-msg-reliable"
                    : "portable-generator-verb-start-msg-unreliable"),
                Disabled = false,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
                Text = Loc.GetString("portable-generator-verb-start"),
            };

            args.Verbs.Add(verb);
        }
    }

    private void GetInteractionVerbs(EntityUid uid, PortableGeneratorComponent component,
        GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var isCurrentlyHV = component.ActiveOutput == PortableGeneratorPowerOutput.HV;
        var msg = isCurrentlyHV ? "portable-generator-switch-output-mv" : "portable-generator-switch-output-hv";

        var fuelGenerator = Comp<FuelGeneratorComponent>(uid);

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                ToggleActiveOutput(uid, component, args.User);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Text = Loc.GetString(msg),
        };

        if (fuelGenerator.On)
        {
            verb.Message = Loc.GetString("portable-generator-verb-switch-output-disabled");
            verb.Disabled = true;
        }

        args.Verbs.Add(verb);
    }

    private void ToggleActiveOutput(EntityUid uid, PortableGeneratorComponent component, EntityUid user)
    {
        var supplier = Comp<PowerSupplierComponent>(uid);
        var nodeContainer = Comp<NodeContainerComponent>(uid);
        var outputMV = (CableDeviceNode) nodeContainer.Nodes["output_mv"];
        var outputHV = (CableDeviceNode) nodeContainer.Nodes["output_hv"];

        if (component.ActiveOutput == PortableGeneratorPowerOutput.HV)
        {
            component.ActiveOutput = PortableGeneratorPowerOutput.MV;
            supplier.Voltage = Voltage.Medium;

            outputMV.Enabled = true;
            outputHV.Enabled = false;
        }
        else
        {
            component.ActiveOutput = PortableGeneratorPowerOutput.HV;
            supplier.Voltage = Voltage.High;

            outputMV.Enabled = false;
            outputHV.Enabled = true;
        }

        _popup.PopupEntity(
            Loc.GetString("portable-generator-switched-output"),
            uid,
            user);

        Dirty(uid, component);

        _nodeGroup.QueueReflood(outputMV);
        _nodeGroup.QueueReflood(outputHV);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PortableGeneratorComponent, FuelGeneratorComponent, AppearanceComponent>();

        while (query.MoveNext(out var uid, out var portGen, out var fuelGen, out var xform))
        {
            UpdateUI(uid, portGen, fuelGen);
        }
    }

    private void UpdateUI(EntityUid uid, PortableGeneratorComponent comp, FuelGeneratorComponent fuelComp)
    {
        if (!_uiSystem.IsUiOpen(uid, GeneratorComponentUiKey.Key))
            return;

        _uiSystem.TrySetUiState(uid, GeneratorComponentUiKey.Key, new PortableGeneratorComponentBuiState(fuelComp));
    }
}

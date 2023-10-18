using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.Power.Generator;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Power.Generator;

/// <summary>
/// Implements power-switchable generators.
/// </summary>
/// <seealso cref="PowerSwitchableGeneratorComponent"/>
/// <seealso cref="PortableGeneratorSystem"/>
/// <seealso cref="GeneratorSystem"/>
public sealed class PowerSwitchableGeneratorSystem : SharedPowerSwitchableGeneratorSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerSwitchableGeneratorComponent, GetVerbsEvent<InteractionVerb>>(GetInteractionVerbs);
    }

    private void GetInteractionVerbs(
        EntityUid uid,
        PowerSwitchableGeneratorComponent component,
        GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var isCurrentlyHV = component.ActiveOutput == PowerSwitchableGeneratorOutput.HV;
        var msg = isCurrentlyHV ? "power-switchable-generator-verb-mv" : "power-switchable-generator-verb-hv";

        var isOn = TryComp(uid, out FuelGeneratorComponent? fuelGenerator) && fuelGenerator.On;

        InteractionVerb verb = new()
        {
            Act = () =>
            {

                var verbIsOn = TryComp(uid, out FuelGeneratorComponent? verbFuelGenerator) && verbFuelGenerator.On;
                if (verbIsOn)
                    return;

                ToggleActiveOutput(uid, args.User, component);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Text = Loc.GetString(msg),
        };

        if (isOn)
        {
            verb.Message = Loc.GetString("power-switchable-generator-verb-disable-on");
            verb.Disabled = true;
        }

        args.Verbs.Add(verb);
    }

    public void ToggleActiveOutput(EntityUid uid, EntityUid user, PowerSwitchableGeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var supplier = Comp<PowerSupplierComponent>(uid);
        var nodeContainer = Comp<NodeContainerComponent>(uid);
        var outputMV = (CableDeviceNode) nodeContainer.Nodes[component.NodeOutputMV];
        var outputHV = (CableDeviceNode) nodeContainer.Nodes[component.NodeOutputHV];

        if (component.ActiveOutput == PowerSwitchableGeneratorOutput.HV)
        {
            component.ActiveOutput = PowerSwitchableGeneratorOutput.MV;
            supplier.Voltage = Voltage.Medium;

            // Switching around the voltage on the power supplier is "enough",
            // but we also want to disconnect the cable nodes so it doesn't show up in power monitors etc.
            outputMV.Enabled = true;
            outputHV.Enabled = false;
        }
        else
        {
            component.ActiveOutput = PowerSwitchableGeneratorOutput.HV;
            supplier.Voltage = Voltage.High;

            outputMV.Enabled = false;
            outputHV.Enabled = true;
        }

        _popup.PopupEntity(
            Loc.GetString("power-switchable-generator-switched-output"),
            uid,
            user);

        _audio.Play(component.SwitchSound, Filter.Pvs(uid), uid, true);

        Dirty(uid, component);

        _nodeGroup.QueueReflood(outputMV);
        _nodeGroup.QueueReflood(outputHV);
    }
}

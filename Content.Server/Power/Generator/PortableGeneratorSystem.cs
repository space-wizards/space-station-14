using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.Power.Generator;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
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

    public override void Initialize()
    {
        base.Initialize();

        // Update UI after main system runs.
        UpdatesAfter.Add(typeof(GeneratorSystem));

        SubscribeLocalEvent<PortableGeneratorComponent, GetVerbsEvent<InteractionVerb>>(GetInteractionVerbs);
    }

    private void GetInteractionVerbs(EntityUid uid, PortableGeneratorComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var isCurrentlyHV = component.ActiveOutput == PortableGeneratorPowerOutput.HV;
        var msg = isCurrentlyHV ? "portable-generator-switch-output-mv" : "portable-generator-switch-output-hv";

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                ToggleActiveOutput(uid, component, args.User);
            },
            Message = Loc.GetString(msg),
            Disabled = false,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Text = Loc.GetString(msg),
        };

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
        var query = EntityQueryEnumerator<PortableGeneratorComponent, FuelGeneratorComponent, TransformComponent>();

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

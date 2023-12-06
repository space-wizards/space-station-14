using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.Power.Generator;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Power.Generator;

/// <summary>
/// Implements server logic for power-switchable devices.
/// </summary>
/// <seealso cref="PowerSwitchableComponent"/>
/// <seealso cref="PortableGeneratorSystem"/>
/// <seealso cref="GeneratorSystem"/>
public sealed class PowerSwitchableSystem : SharedPowerSwitchableSystem
{
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerSwitchableComponent, GetVerbsEvent<InteractionVerb>>(GetVerbs);
    }

    private void GetVerbs(EntityUid uid, PowerSwitchableComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var voltage = VoltageColor(GetNextVoltage(uid, comp));
        var msg = Loc.GetString("power-switchable-switch-voltage", ("voltage", voltage));

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                // don't need to check it again since if its disabled server wont let the verb act
                Cycle(uid, args.User, comp);
            },
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Text = msg
        };

        var ev = new SwitchPowerCheckEvent();
        RaiseLocalEvent(uid, ref ev);
        if (ev.DisableMessage != null)
        {
            verb.Message = ev.DisableMessage;
            verb.Disabled = true;
        }

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Cycles voltage then updates nodes and optionally power supplier to match it.
    /// </summary>
    public void Cycle(EntityUid uid, EntityUid user, PowerSwitchableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        // no sound spamming
        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        comp.ActiveIndex = NextIndex(uid, comp);
        Dirty(uid, comp);

        var voltage = GetVoltage(uid, comp);

        if (TryComp<PowerSupplierComponent>(uid, out var supplier))
        {
            // convert to nodegroupid (goofy server Voltage enum is just alias for it)
            switch (voltage)
            {
                case SwitchableVoltage.HV:
                    supplier.Voltage = Voltage.High;
                    break;
                case SwitchableVoltage.MV:
                    supplier.Voltage = Voltage.Medium;
                    break;
                case SwitchableVoltage.LV:
                    supplier.Voltage = Voltage.Apc;
                    break;
            }
        }

        // Switching around the voltage on the power supplier is "enough",
        // but we also want to disconnect the cable nodes so it doesn't show up in power monitors etc.
        var nodeContainer = Comp<NodeContainerComponent>(uid);
        foreach (var cable in comp.Cables)
        {
            var node = (CableDeviceNode) nodeContainer.Nodes[cable.Node];
            node.Enabled = cable.Voltage == voltage;
            _nodeGroup.QueueReflood(node);
        }

        var popup = Loc.GetString(comp.SwitchText, ("voltage", VoltageString(voltage)));
        _popup.PopupEntity(popup, uid, user);

        _audio.PlayPvs(comp.SwitchSound, uid);

        _useDelay.TryResetDelay((uid, useDelay));
    }
}

/// <summary>
/// Raised on a <see cref="PowerSwitchableComponent"/> to see if its verb should work.
/// If <see cref="DisableMessage"/> is non-null, the verb is disabled with that as the message.
/// </summary>
[ByRefEvent]
public record struct SwitchPowerCheckEvent(string? DisableMessage = null);

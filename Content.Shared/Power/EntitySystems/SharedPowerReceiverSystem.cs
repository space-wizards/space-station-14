using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Power.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Power.EntitySystems;

public abstract partial class SharedPowerReceiverSystem : EntitySystem
{
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPowerNetSystem _net = default!;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;
    [Dependency] protected EntityQuery<PowerReceiverComponent> RecQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerSwitchComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchPowerVerb);
    }

    private void AddSwitchPowerVerb(EntityUid uid, PowerSwitchComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if(!args.CanAccess || !args.CanInteract)
            return;

        if (!_handsQuery.HasComp(args.User))
            return;

        if (!RecQuery.TryGetComponent(uid, out var receiver))
            return;

        if (!receiver.NeedsPower)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TogglePower(uid, user: args.User);
            },
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Text = Loc.GetString("power-switch-component-toggle-verb"),
            Priority = -3
        };
        args.Verbs.Add(verb);
    }

    public abstract bool ResolveApc(EntityUid entity, [NotNullWhen(true)] ref PowerReceiverComponent? component);

    public void SetNeedsPower(EntityUid uid, bool value, PowerReceiverComponent? receiver = null)
    {
        if (!ResolveApc(uid, ref receiver) || receiver.NeedsPower == value)
            return;

        receiver.NeedsPower = value;
        Dirty(uid, receiver);
    }

    public void SetPowerDisabled(EntityUid uid, bool value, PowerReceiverComponent? receiver = null)
    {
        if (!ResolveApc(uid, ref receiver) || !receiver.Enabled == value)
            return;

        receiver.Enabled = !value;
        Dirty(uid, receiver);
    }

    /// <summary>
    /// Turn this machine on or off.
    /// Returns true if we turned it on, false if we turned it off.
    /// </summary>
    public bool TogglePower(EntityUid uid, bool playSwitchSound = true, PowerReceiverComponent? receiver = null, EntityUid? user = null)
    {
        if (!ResolveApc(uid, ref receiver))
            return true;

        // it'll save a lot of confusion if 'always powered' means 'always powered'
        if (!receiver.NeedsPower)
        {
            var powered = _net.IsPoweredCalculate(receiver);

            // Server won't raise it here as it can raise the load event later with NeedsPower?
            // This is mostly here for clientside predictions.
            if (receiver.Powered != powered)
            {
                RaisePower((uid, receiver));
            }

            SetPowerDisabled(uid, false, receiver);
            return true;
        }

        SetPowerDisabled(uid, receiver.Enabled, receiver);

        if (user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value):player} hit power button on {ToPrettyString(uid)}, it's now {(receiver.Enabled ? "on" : "off")}");

        if (playSwitchSound)
        {
            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg"), uid, user: user,
                AudioParams.Default.WithVolume(-2f));
        }

        if (_netMan.IsClient && !receiver.Enabled)
        {
            var powered = _net.IsPoweredCalculate(receiver);

            // Server won't raise it here as it can raise the load event later with NeedsPower?
            // This is mostly here for clientside predictions.
            if (receiver.Powered != powered)
            {
                receiver.Powered = powered;
                RaisePower((uid, receiver));
            }
        }

        return receiver.Enabled;
    }

    protected virtual void RaisePower(Entity<PowerReceiverComponent> entity)
    {
        // NOOP on server because client has 0 idea of load so we can't raise it properly in shared.
    }

    /// <summary>
    /// Sets the power load of this power receiver.
    /// </summary>
    public void SetLoad(Entity<PowerReceiverComponent?> entity, float load)
    {
        if (!ResolveApc(entity.Owner, ref entity.Comp))
            return;

        entity.Comp.DesiredPower = load;
    }

    /// <summary>
    /// Checks if entity is APC-powered device, and if it have power.
    /// </summary>
    public bool IsPowered(Entity<PowerReceiverComponent?> entity)
    {
        if (!ResolveApc(entity.Owner, ref entity.Comp))
            return true;

        return entity.Comp.Powered;
    }

    protected string GetExamineText(bool powered)
    {
        return Loc.GetString("power-receiver-component-on-examine-main",
                                ("stateText", Loc.GetString(powered
                                    ? "power-receiver-component-on-examine-powered"
                                    : "power-receiver-component-on-examine-unpowered")));
    }
}

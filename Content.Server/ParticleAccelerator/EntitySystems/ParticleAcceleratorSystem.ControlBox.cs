using Content.Server.ParticleAccelerator.Components;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Machines.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Utility;
using System.Diagnostics;
using Content.Server.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.ParticleAccelerator;
using Content.Shared.Machines.Events;

namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private void InitializeControlBoxSystem()
    {
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, PowerChangedEvent>(OnControlBoxPowerChange);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ParticleAcceleratorSetEnableMessage>(OnUISetEnableMessage);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ParticleAcceleratorSetPowerStateMessage>(OnUISetPowerMessage);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, ParticleAcceleratorRescanPartsMessage>(OnUIRescanMessage);
        SubscribeLocalEvent<ParticleAcceleratorControlBoxComponent, MultipartMachineAssemblyStateChanged>(OnMachineAssembledChanged);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ParticleAcceleratorControlBoxComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (controller.Firing && curTime >= controller.NextFire)
                Fire(uid, curTime, controller);
        }
    }

    [Conditional("DEBUG")]
    private void EverythingIsWellToFire(ParticleAcceleratorControlBoxComponent controller,
        Entity<MultipartMachineComponent> machine)
    {
        DebugTools.Assert(controller.Powered);
        DebugTools.Assert(controller.SelectedStrength != ParticleAcceleratorPowerState.Standby);
        DebugTools.Assert(machine.Comp.IsAssembled);
    }

    public void Fire(EntityUid uid, TimeSpan curTime, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.LastFire = curTime;
        comp.NextFire = curTime + comp.ChargeTime;

        if (!TryComp<MultipartMachineComponent>(uid, out var machineComp))
            return;

        var machine = (uid, machineComp);
        EverythingIsWellToFire(comp, machine);

        var strength = comp.SelectedStrength;

        FireEmitter(_multipartMachine.GetPartEntity(machine, AcceleratorParts.PortEmitter)!.Value, strength);
        FireEmitter(_multipartMachine.GetPartEntity(machine, AcceleratorParts.ForeEmitter)!.Value, strength);
        FireEmitter(_multipartMachine.GetPartEntity(machine, AcceleratorParts.StarboardEmitter)!.Value, strength);
    }

    public void SwitchOn(EntityUid uid, EntityUid? user = null, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        DebugTools.Assert(_multipartMachine.IsAssembled((uid, null)));

        if (comp.Enabled || !comp.CanBeEnabled)
            return;

        if (user is { } player)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} has turned {ToPrettyString(uid)} on");

        comp.Enabled = true;
        UpdatePowerDraw(uid, comp);

        if (!TryComp<PowerConsumerComponent>(_multipartMachine.GetPartEntity(uid, AcceleratorParts.PowerBox), out var powerConsumer)
            || powerConsumer.ReceivedPower >= powerConsumer.DrawRate * ParticleAcceleratorControlBoxComponent.RequiredPowerRatio)
        {
            PowerOn(uid, comp);
        }

        UpdateUI(uid, comp);
    }

    public void SwitchOff(EntityUid uid, EntityUid? user = null, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!comp.Enabled)
            return;

        if (user is { } player)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} has turned {ToPrettyString(uid)} off");

        comp.Enabled = false;
        SetStrength(uid, ParticleAcceleratorPowerState.Standby, user, comp);
        UpdatePowerDraw(uid, comp);
        PowerOff(uid, comp);
        UpdateUI(uid, comp);
    }

    public void PowerOn(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        DebugTools.Assert(comp.Enabled);
        DebugTools.Assert(_multipartMachine.IsAssembled((uid, null)));

        if (comp.Powered)
            return;

        comp.Powered = true;
        UpdatePowerDraw(uid, comp);
        UpdateFiring(uid, comp);
        UpdatePartVisualStates(uid, comp);
        UpdateUI(uid, comp);
    }

    public void PowerOff(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!comp.Powered)
            return;

        comp.Powered = false;
        UpdatePowerDraw(uid, comp);
        UpdateFiring(uid, comp);
        UpdatePartVisualStates(uid, comp);
        UpdateUI(uid, comp);
    }

    public void SetStrength(EntityUid uid, ParticleAcceleratorPowerState strength, EntityUid? user = null, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (comp.StrengthLocked)
            return;

        strength = (ParticleAcceleratorPowerState) MathHelper.Clamp(
            (int) strength,
            (int) ParticleAcceleratorPowerState.Standby,
            (int) comp.MaxStrength
        );

        if (strength == comp.SelectedStrength)
            return;

        if (user is { } player)
        {
            var impact = strength switch
            {
                ParticleAcceleratorPowerState.Standby => LogImpact.Low,
                ParticleAcceleratorPowerState.Level0
                    or ParticleAcceleratorPowerState.Level1
                    or ParticleAcceleratorPowerState.Level2 => LogImpact.Medium,
                ParticleAcceleratorPowerState.Level3 => LogImpact.Extreme,
                _ => throw new IndexOutOfRangeException(nameof(strength)),
            };

            _adminLogger.Add(LogType.Action, impact, $"{ToPrettyString(player):player} has set the strength of {ToPrettyString(uid)} to {strength}");


            var alertMinPowerState = (ParticleAcceleratorPowerState)_cfg.GetCVar(CCVars.AdminAlertParticleAcceleratorMinPowerState);
            if (strength >= alertMinPowerState)
            {
                var pos = Transform(uid);
                if (_gameTiming.CurTime > comp.EffectCooldown)
                {
                    _chat.SendAdminAlert(player,
                        Loc.GetString("particle-accelerator-admin-power-strength-warning",
                        ("machine", ToPrettyString(uid)),
                        ("powerState", GetPANumericalLevel(strength)),
                        ("coordinates", pos.Coordinates)));
                    _audio.PlayGlobal("/Audio/Misc/adminlarm.ogg",
                        Filter.Empty().AddPlayers(_adminManager.ActiveAdmins),
                        false,
                        AudioParams.Default.WithVolume(-8f));
                    comp.EffectCooldown = _gameTiming.CurTime + comp.CooldownDuration;
                }
            }
        }

        comp.SelectedStrength = strength;
        UpdateAppearance(uid, comp);
        UpdatePartVisualStates(uid, comp);

        if (comp.Enabled)
        {
            UpdatePowerDraw(uid, comp);
            UpdateFiring(uid, comp);
        }
    }

    private void UpdateFiring(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!comp.Powered || comp.SelectedStrength < ParticleAcceleratorPowerState.Level0)
        {
            comp.Firing = false;
            return;
        }

        if (!TryComp<MultipartMachineComponent>(uid, out var machine))
            return;

        EverythingIsWellToFire(comp, (uid, machine));

        var curTime = _gameTiming.CurTime;
        comp.LastFire = curTime;
        comp.NextFire = curTime + comp.ChargeTime;
        comp.Firing = true;
    }

    private void UpdatePowerDraw(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!TryComp<PowerConsumerComponent>(_multipartMachine.GetPartEntity(uid, AcceleratorParts.PowerBox), out var powerConsumer))
            return;

        var powerDraw = comp.BasePowerDraw;
        if (comp.Enabled)
            powerDraw += comp.LevelPowerDraw * (int) comp.SelectedStrength;

        powerConsumer.DrawRate = powerDraw;
    }

    public void UpdateUI(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!_uiSystem.HasUi(uid, ParticleAcceleratorControlBoxUiKey.Key))
            return;

        var draw = 0f;
        var receive = 0f;

        if (TryComp<PowerConsumerComponent>(_multipartMachine.GetPartEntity(uid, AcceleratorParts.PowerBox), out var powerConsumer))
        {
            draw = powerConsumer.DrawRate;
            receive = powerConsumer.ReceivedPower;
        }

        if (!TryComp<MultipartMachineComponent>(uid, out var machineComp))
            return;

        var machine = (uid, machineComp);

        var uiState = new ParticleAcceleratorUIState(
            machineComp.IsAssembled,
            comp.Enabled,
            comp.SelectedStrength,
            (int)draw,
            (int)receive,
            _multipartMachine.HasPart(machine, AcceleratorParts.StarboardEmitter),
            _multipartMachine.HasPart(machine, AcceleratorParts.ForeEmitter),
            _multipartMachine.HasPart(machine, AcceleratorParts.PortEmitter),
            _multipartMachine.HasPart(machine, AcceleratorParts.PowerBox),
            _multipartMachine.HasPart(machine, AcceleratorParts.FuelChamber),
            _multipartMachine.HasPart(machine, AcceleratorParts.EndCap),
            comp.InterfaceDisabled,
            comp.MaxStrength,
            comp.StrengthLocked
        );

        _uiSystem.SetUiState(uid, ParticleAcceleratorControlBoxUiKey.Key, uiState);
    }

    private void UpdateAppearance(EntityUid uid, ParticleAcceleratorControlBoxComponent? comp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _appearanceSystem.SetData(
            uid,
            ParticleAcceleratorVisuals.VisualState,
            TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered
                ? ParticleAcceleratorVisualState.Unpowered
                : (ParticleAcceleratorVisualState) comp.SelectedStrength,
            appearance
        );
    }

    private void UpdatePartVisualStates(EntityUid uid, ParticleAcceleratorControlBoxComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        var state = controller.Powered ? (ParticleAcceleratorVisualState) controller.SelectedStrength : ParticleAcceleratorVisualState.Unpowered;

        if (!TryComp<MultipartMachineComponent>(uid, out var machineComp))
            return;

        var machine = (uid, machineComp);

        // UpdatePartVisualState(ControlBox); (We are the control box)
        if (_multipartMachine.TryGetPartEntity(machine, AcceleratorParts.FuelChamber, out var fuelChamber))
            _appearanceSystem.SetData(fuelChamber.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (_multipartMachine.TryGetPartEntity(machine, AcceleratorParts.PowerBox, out var powerBox))
            _appearanceSystem.SetData(powerBox.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (_multipartMachine.TryGetPartEntity(machine, AcceleratorParts.PortEmitter, out var portEmitter))
            _appearanceSystem.SetData(portEmitter.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (_multipartMachine.TryGetPartEntity(machine, AcceleratorParts.ForeEmitter, out var foreEmitter))
            _appearanceSystem.SetData(foreEmitter.Value, ParticleAcceleratorVisuals.VisualState, state);
        if (_multipartMachine.TryGetPartEntity(machine, AcceleratorParts.StarboardEmitter, out var starboardEmitter))
            _appearanceSystem.SetData(starboardEmitter.Value, ParticleAcceleratorVisuals.VisualState, state);
        //no endcap because it has no powerlevel-sprites
    }

    /// <summary>
    /// Handles when a multipart machine has had some assembled/disassembled state change, or had parts added/removed.
    /// </summary>
    /// <param name="ent">Multipart machine entity</param>
    /// <param name="args">Args for this event</param>
    private void OnMachineAssembledChanged(Entity<ParticleAcceleratorControlBoxComponent> ent, ref MultipartMachineAssemblyStateChanged args)
    {
        if (args.IsAssembled)
        {
            UpdatePowerDraw(ent, ent.Comp);
            UpdateUI(ent, ent.Comp);
        }
        else
        {
            if (ent.Comp.Powered)
            {
                SwitchOff(ent, args.User, ent.Comp);
            }
            else
            {
                UpdateAppearance(ent, ent.Comp);
                UpdateUI(ent, ent.Comp);
            }

            // Because the parts are already removed from the multipart machine, updating the visual appearance won't find any valid entities.
            // We know which parts have been removed so we can update the visual state to unpowered in a more manual way here.
            foreach (var (key, part) in args.PartsRemoved)
            {
                if (key is AcceleratorParts.EndCap)
                    continue; // No endcap powerlevel-sprites

                _appearanceSystem.SetData(part, ParticleAcceleratorVisuals.VisualState, ParticleAcceleratorVisualState.Unpowered);
            }
        }
    }

    // This is the power state for the PA control box itself.
    // Keep in mind that the PA itself can keep firing as long as the HV cable under the power box has... power.
    private void OnControlBoxPowerChange(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ref PowerChangedEvent args)
    {
        UpdateAppearance(uid, comp);

        if (!args.Powered)
            _uiSystem.CloseUi(uid, ParticleAcceleratorControlBoxUiKey.Key);
    }

    private void OnUISetEnableMessage(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ParticleAcceleratorSetEnableMessage msg)
    {
        if (!ParticleAcceleratorControlBoxUiKey.Key.Equals(msg.UiKey))
            return;
        if (comp.InterfaceDisabled)
            return;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered)
            return;

        if (msg.Enabled)
        {
            if (_multipartMachine.IsAssembled((uid, null)))
                SwitchOn(uid, msg.Actor, comp);
        }
        else
            SwitchOff(uid, msg.Actor, comp);

        UpdateUI(uid, comp);
    }

    private void OnUISetPowerMessage(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ParticleAcceleratorSetPowerStateMessage msg)
    {
        if (!ParticleAcceleratorControlBoxUiKey.Key.Equals(msg.UiKey))
            return;
        if (comp.InterfaceDisabled)
            return;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered)
            return;

        SetStrength(uid, msg.State, msg.Actor, comp);

        UpdateUI(uid, comp);
    }

    private void OnUIRescanMessage(EntityUid uid, ParticleAcceleratorControlBoxComponent comp, ParticleAcceleratorRescanPartsMessage msg)
    {
        if (!ParticleAcceleratorControlBoxUiKey.Key.Equals(msg.UiKey))
            return;
        if (comp.InterfaceDisabled)
            return;
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPower) && !apcPower.Powered)
            return;

        if (!TryComp<MultipartMachineComponent>(uid, out var machineComp))
            return;

        // User has requested a manual rescan of the machine, if anything HAS changed that the multipart
        // machine system has missed then a AssemblyStateChanged event will be raised at the machine.
        var machine = new Entity<MultipartMachineComponent>(uid, machineComp);
        _multipartMachine.Rescan(machine, msg.Actor);
    }

    public static int GetPANumericalLevel(ParticleAcceleratorPowerState state)
    {
        return state switch
        {
            ParticleAcceleratorPowerState.Level0 => 1,
            ParticleAcceleratorPowerState.Level1 => 2,
            ParticleAcceleratorPowerState.Level2 => 3,
            ParticleAcceleratorPowerState.Level3 => 4,
            _ => 0
        };
    }
}

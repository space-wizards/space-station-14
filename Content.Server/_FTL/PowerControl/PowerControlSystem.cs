using Content.Server._FTL.Areas;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared._FTL.PowerControl;
using Robust.Server.GameObjects;

namespace Content.Server._FTL.PowerControl;

/// <inheritdoc/>
public sealed class PowerControlSystem : SharedPowerControlSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly ApcSystem _apc = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PowerControlComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<PowerControlComponent, ToggleApcMessage>(OnApcToggleRequestMessage);
        base.Initialize();
    }

    private void OnApcToggleRequestMessage(EntityUid uid, PowerControlComponent component, ToggleApcMessage message)
    {
        var apc = message.ApcEntity;
;
        _apc.ApcToggleBreaker(apc);
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, PowerControlComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        var state = new PowerControlState(_areas.GetAreasOnGrid(xform.GridUid));
        _userInterface.TrySetUiState(uid, PowerControlUiKey.Key, state);
    }

    private void OnToggleInterface(EntityUid uid, PowerControlComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }
}

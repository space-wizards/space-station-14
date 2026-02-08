using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Visuals;
using Content.Shared.DeviceLinking.Events;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using static Content.Shared.DeviceLinking.Visuals.RngDeviceVisuals;
using Content.Shared.UserInterface;
using Content.Shared.DeviceLinking.Systems;

namespace Content.Client.DeviceLinking.Systems;

/// <summary>
/// Client-side system for RNG device functionality
/// </summary>
public sealed class RngDeviceSystem : SharedRngDeviceSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RngDeviceComponent, AfterAutoHandleStateEvent>(OnRngDeviceState);
        SubscribeLocalEvent<RngDeviceVisualsComponent, RollEvent>(OnRoll);
    }

    private void OnRngDeviceState(Entity<RngDeviceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Update any open BUIs when component data changes
        if (_ui.TryGetOpenUi(ent.Owner, RngDeviceUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    private void OnRoll(Entity<RngDeviceVisualsComponent> ent, ref RollEvent args)
    {
        if (args.Handled)
            return;

        PredictRoll(ent, args.Outputs, args.User);
        args = args.WithHandled(true);
    }

    // Predicts a roll on the client side for responsive UI
    private void PredictRoll(Entity<RngDeviceVisualsComponent> ent, int outputs, EntityUid? user = null)
    {
        int roll;
        // Only use target number for percentile dice (outputs == 2)
        if (outputs == 2 && TryComp<RngDeviceComponent>(ent, out var rngComp))
        {
            // Use the overload that takes targetNumber
            (roll, _) = GenerateRoll(outputs, rngComp.TargetNumber);
        }
        else
        {
            // Use the original overload without targetNumber
            (roll, _) = GenerateRoll(outputs);
        }

        // Update visuals with the predicted roll
        UpdateVisualState(ent, outputs, roll);

        // Show popup
        var popupString = Loc.GetString("rng-device-rolled", ("value", roll));
        _popup.PopupPredicted(popupString, ent, user);
    }

    private void UpdateVisualState(Entity<RngDeviceVisualsComponent> ent, int outputs, int roll)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        // Get the StatePrefix from the RngDeviceComponent
        if (!TryComp<RngDeviceComponent>(ent, out var rngComp))
            return;

        var stateNumber = outputs switch
        {
            2 => roll == 100 ? 0 : (roll / 10) * 10,  // Show "00" for 100, otherwise round down to nearest 10
            10 => roll == 10 ? 0 : roll,  // Show "0" for 10
            _ => roll
        };
        _appearance.SetData(ent, State, $"{rngComp.StatePrefix}_{stateNumber}", appearance);
    }
}

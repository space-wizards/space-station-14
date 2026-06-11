using Content.Shared.DeviceLinking.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;

namespace Content.Shared.DeviceLinking.Systems;

/// <summary>
/// Shared handling for <see cref="CableRelayComponent"/>: drives the switch state and interface so the UI can be
/// predicted. The actual cable severing lives in the server system through <see cref="ApplyCables"/>.
/// </summary>
public abstract class SharedCableRelaySystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    private static readonly HashSet<CableType> SupportedTypes =
        new() { CableType.HighVoltage, CableType.MediumVoltage, CableType.Apc };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CableRelayComponent, AfterActivatableUIOpenEvent>(OnUiOpened);
        SubscribeLocalEvent<CableRelayComponent, CableRelayToggleMessage>(OnToggleMessage);
        SubscribeLocalEvent<CableRelayComponent, CableRelaySetCableTypeMessage>(OnSetCableTypeMessage);
    }

    private void OnUiOpened(Entity<CableRelayComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    private void OnToggleMessage(Entity<CableRelayComponent> ent, ref CableRelayToggleMessage args)
    {
        Toggle(ent);
    }

    private void OnSetCableTypeMessage(Entity<CableRelayComponent> ent, ref CableRelaySetCableTypeMessage args)
    {
        if (!SupportedTypes.Contains(args.CableType))
            return;

        if (args.Enabled)
            ent.Comp.AffectedTypes.Add(args.CableType);
        else
            ent.Comp.AffectedTypes.Remove(args.CableType);

        Dirty(ent);
        ApplyCables(ent);
        UpdateUi(ent);
    }

    protected void Toggle(Entity<CableRelayComponent> ent)
    {
        // Switching on needs power; switching off is always allowed.
        if (!ent.Comp.Severed && !_power.IsPowered(ent.Owner))
        {
            UpdateUi(ent);
            return;
        }

        ent.Comp.Severed = !ent.Comp.Severed;
        Dirty(ent);
        ApplyCables(ent);
        UpdateUi(ent);
    }

    /// <summary>
    /// Applies the relay's switch state to the cables on its tile. Server-only; a no-op on the client since the
    /// powernet is not predicted.
    /// </summary>
    protected virtual void ApplyCables(Entity<CableRelayComponent> ent) { }

    protected void UpdateUi(Entity<CableRelayComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, CableRelayUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, CableRelayUiKey.Key,
            new CableRelayBoundUserInterfaceState(_power.IsPowered(ent.Owner), ent.Comp.Severed, ent.Comp.AffectedTypes));
    }
}

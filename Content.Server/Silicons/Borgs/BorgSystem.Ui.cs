using System.Linq;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.NameIdentifier;
using Content.Shared.PowerCell.Components;
using Content.Shared.Preferences;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    public void InitializeUI()
    {
        SubscribeLocalEvent<BorgChassisComponent, BeforeActivatableUIOpenEvent>(OnBeforeBorgUiOpen);
        SubscribeLocalEvent<BorgChassisComponent, BorgEjectBrainBuiMessage>(OnEjectBrainBuiMessage);
        SubscribeLocalEvent<BorgChassisComponent, BorgEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<BorgChassisComponent, BorgSetNameBuiMessage>(OnSetNameBuiMessage);
        SubscribeLocalEvent<BorgChassisComponent, BorgRemoveModuleBuiMessage>(OnRemoveModuleBuiMessage);
    }

    private void OnBeforeBorgUiOpen(EntityUid uid, BorgChassisComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUI(uid, component);
    }

    private void OnEjectBrainBuiMessage(EntityUid uid, BorgChassisComponent component, BorgEjectBrainBuiMessage args)
    {
        if (component.BrainEntity is not { } brain)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} removed brain {ToPrettyString(brain)} from borg {ToPrettyString(uid)}");
        _container.Remove(brain, component.BrainContainer);
        _hands.TryPickupAnyHand(args.Actor, brain);
        UpdateUI(uid, component);
    }

    private void OnEjectBatteryBuiMessage(EntityUid uid, BorgChassisComponent component, BorgEjectBatteryBuiMessage args)
    {
        if (!TryComp<PowerCellSlotComponent>(uid, out var slotComp) ||
            !Container.TryGetContainer(uid, slotComp.CellSlotId, out var container) ||
            !container.ContainedEntities.Any())
        {
            return;
        }

        var ents = Container.EmptyContainer(container);
        _hands.TryPickupAnyHand(args.Actor, ents.First());
    }

    private void OnSetNameBuiMessage(EntityUid uid, BorgChassisComponent component, BorgSetNameBuiMessage args)
    {
        if (args.Name.Length > HumanoidCharacterProfile.MaxNameLength ||
            args.Name.Length == 0 ||
            string.IsNullOrWhiteSpace(args.Name) ||
            string.IsNullOrEmpty(args.Name))
        {
            return;
        }

        var name = args.Name.Trim();
        if (TryComp<NameIdentifierComponent>(uid, out var identifier))
            name = $"{name} {identifier.FullIdentifier}";

        var metaData = MetaData(uid);

        // don't change the name if the value doesn't actually change
        if (metaData.EntityName.Equals(name, StringComparison.InvariantCulture))
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Actor):player} set borg \"{ToPrettyString(uid)}\"'s name to: {name}");
        _metaData.SetEntityName(uid, name, metaData);
    }

    private void OnRemoveModuleBuiMessage(EntityUid uid, BorgChassisComponent component, BorgRemoveModuleBuiMessage args)
    {
        var module = GetEntity(args.Module);

        if (!component.ModuleContainer.Contains(module))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} removed module {ToPrettyString(module)} from borg {ToPrettyString(uid)}");
        _container.Remove(module, component.ModuleContainer);
        _hands.TryPickupAnyHand(args.Actor, module);

        UpdateUI(uid, component);
    }

    public void UpdateUI(EntityUid uid, BorgChassisComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var chargePercent = 0f;
        var hasBattery = false;
        if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
        {
            hasBattery = true;
            chargePercent = battery.CurrentCharge / battery.MaxCharge;
        }

        var state = new BorgBuiState(chargePercent, hasBattery);
        _ui.SetUiState(uid, BorgUiKey.Key, state);
    }
}

using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Borgs;

public abstract partial class SharedBorgSystem
{
    // CCvar
    private int _maxNameLength;

    public void InitializeUI()
    {
        SubscribeLocalEvent<BorgChassisComponent, BorgEjectBrainBuiMessage>(OnEjectBrainBuiMessage);
        SubscribeLocalEvent<BorgChassisComponent, BorgEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<BorgChassisComponent, BorgSetNameBuiMessage>(OnSetNameBuiMessage);
        SubscribeLocalEvent<BorgChassisComponent, BorgRemoveModuleBuiMessage>(OnRemoveModuleBuiMessage);

        Subs.CVar(_configuration, CCVars.MaxNameLength, value => _maxNameLength = value, true);
    }

    public virtual void UpdateUI(Entity<BorgChassisComponent?> chassis) { }

    private void OnEjectBrainBuiMessage(Entity<BorgChassisComponent> chassis, ref BorgEjectBrainBuiMessage args)
    {
        if (chassis.Comp.BrainEntity is not { } brain)
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{args.Actor} removed brain {brain} from borg {chassis.Owner}");
        _container.Remove(brain, chassis.Comp.BrainContainer);
        _hands.TryPickupAnyHand(args.Actor, brain);
    }

    private void OnEjectBatteryBuiMessage(Entity<BorgChassisComponent> chassis, ref BorgEjectBatteryBuiMessage args)
    {
        if (_powerCell.TryEjectBatteryFromSlot(chassis.Owner, out var powerCell, args.Actor))
            _hands.TryPickupAnyHand(args.Actor, powerCell.Value);
    }

    private void OnSetNameBuiMessage(Entity<BorgChassisComponent> chassis, ref BorgSetNameBuiMessage args)
    {
        if (args.Name.Length > _maxNameLength ||
            args.Name.Length == 0 ||
            string.IsNullOrWhiteSpace(args.Name) ||
            string.IsNullOrEmpty(args.Name))
        {
            return;
        }

        var name = args.Name.Trim();

        var metaData = MetaData(chassis);

        // don't change the name if the value doesn't actually change
        if (metaData.EntityName.Equals(name, StringComparison.InvariantCulture))
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{args.Actor} set borg \"{chassis.Owner}\"'s name to: {name}");
        _metaData.SetEntityName(chassis, name, metaData);
    }

    private void OnRemoveModuleBuiMessage(Entity<BorgChassisComponent> chassis, ref BorgRemoveModuleBuiMessage args)
    {
        var module = GetEntity(args.Module);

        if (!chassis.Comp.ModuleContainer.Contains(module))
            return;

        if (!CanRemoveModule((module, Comp<BorgModuleComponent>(module))))
            return;

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{args.Actor} removed module {module} from borg {chassis.Owner}");
        _container.Remove(module, chassis.Comp.ModuleContainer);
        _hands.TryPickupAnyHand(args.Actor, module);
    }
}

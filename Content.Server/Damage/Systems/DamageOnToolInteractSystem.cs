using Content.Server.Administration.Logs;
using Content.Server.Damage.Components;
using Content.Shared.Administration.Logs.Payloads;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using ItemToggleComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleComponent;
using System.Linq;

namespace Content.Server.Damage.Systems
{
    public sealed partial class DamageOnToolInteractSystem : EntitySystem
    {
        [Dependency] private Shared.Damage.Systems.DamageableSystem _damageableSystem = default!;
        [Dependency] private IAdminLogManager _adminLogger = default!;
        [Dependency] private SharedToolSystem _toolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageOnToolInteractComponent, InteractUsingEvent>(OnInteracted);
        }

        private void OnInteracted(EntityUid uid, DamageOnToolInteractComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<ItemToggleComponent>(args.Used, out var itemToggle))
                return;

            if (component.WeldingDamage is {} weldingDamage
            && TryComp(args.Used, out WelderComponent? welder)
            && itemToggle.Activated
            && !welder.TankSafe)
            {
                if (_damageableSystem.TryChangeDamage(args.Target, weldingDamage, out var dmg, origin: args.User))
                {
                    var byType = dmg.DamageDict
                        .Select(kvp => new DamageEntrySnapshot(kvp.Key.Id, Math.Abs(kvp.Value.Int())))
                        .ToList();
                    _adminLogger.Add(LogType.Damaged,
                        $"{args.User:user} used {args.Used:used} as a welder to deal {dmg.GetTotal():damage} damage to {args.Target:target}",
                        new CombatDamageLogPayload(
                            MetaData(args.Used).EntityPrototype?.ID,
                            null,
                            byType,
                            Math.Abs(dmg.GetTotal().Int())));
                }

                args.Handled = true;
            }
            else if (component.DefaultDamage is {} damage
                && _toolSystem.HasQuality(args.Used, component.Tools))
            {
                if (_damageableSystem.TryChangeDamage(args.Target, damage, out var dmg, origin: args.User))
                {
                    var byType = dmg.DamageDict
                        .Select(kvp => new DamageEntrySnapshot(kvp.Key.Id, Math.Abs(kvp.Value.Int())))
                        .ToList();
                    _adminLogger.Add(LogType.Damaged,
                        $"{args.User:user} used {args.Used:used} as a tool to deal {dmg.GetTotal():damage} damage to {args.Target:target}",
                        new CombatDamageLogPayload(
                            MetaData(args.Used).EntityPrototype?.ID,
                            null,
                            byType,
                            Math.Abs(dmg.GetTotal().Int())));
                }

                args.Handled = true;
            }
        }
    }
}

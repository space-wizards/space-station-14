using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using ItemToggleComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleComponent;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnToolInteractSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnToolInteractComponent, InteractUsingEvent>(OnInteracted);
    }

    private void OnInteracted(Entity<DamageOnToolInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ItemToggleComponent>(args.Used, out var itemToggle))
            return;

        if (ent.Comp.WeldingDamage is { } weldingDamage
            && TryComp(args.Used, out WelderComponent? welder)
            && itemToggle.Activated
            && !welder.TankSafe)
        {
            if (_damageableSystem.TryChangeDamage(args.Target, weldingDamage, out var dmg, origin: args.User))
            {
                _adminLogger.Add(LogType.Damaged,
                    $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a welder to deal {dmg.GetTotal():damage} damage to {ToPrettyString(args.Target):target}");
            }

            args.Handled = true;
        }
        else if (ent.Comp.DefaultDamage is { } damage
            && _toolSystem.HasQuality(args.Used, ent.Comp.Tools))
        {
            if (_damageableSystem.TryChangeDamage(args.Target, damage, out var dmg, origin: args.User))
            {
                _adminLogger.Add(LogType.Damaged,
                    $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a tool to deal {dmg.GetTotal():damage} damage to {ToPrettyString(args.Target):target}");
            }

            args.Handled = true;
        }
    }
}

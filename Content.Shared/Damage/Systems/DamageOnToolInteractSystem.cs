using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageOnToolInteractSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    [SubscribeLocalEvent]
    private void OnInteracted(Entity<DamageOnToolInteractComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        foreach (var (quality, damage) in ent.Comp.Damage)
        {
            if (!_tool.HasQuality(args.Used, quality))
                continue;

            if (_damageable.TryChangeDamage(args.Target, damage, out var dmg, origin: args.User))
            {
                _adminLogger.Add(LogType.Damaged,
                    $"{ToPrettyString(args.User):user} used {ToPrettyString(args.Used):used} as a tool to deal {dmg.GetTotal():damage} damage to {ToPrettyString(args.Target):target}");
            }

            args.Handled = true;
            break;
        }
    }
}

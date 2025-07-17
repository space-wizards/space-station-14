using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Timing;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.DeadSpace.Censer;
using Content.Server.Bible.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;
using Content.Shared.Popups;

namespace Content.Server.DeadSpace.Censer;

public sealed class CenserSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CenserComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CenserComponent, CenserDoAfterEvent>(OnCenserDoAfter);
    }

    private void OnCenserDoAfter(Entity<CenserComponent> ent, ref CenserDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out var solution))
            return;

        if (!TryComp(ent, out UseDelayComponent? useDelay) || _delay.IsDelayed((ent, useDelay)))
            return;

        if (_solutionContainer.GetTotalPrototypeQuantity(ent, ent.Comp.Reagent) < ent.Comp.Consumption)
        {
            var notEnoughReagentMessage = Loc.GetString("censer-notenought-reagent");
            _popupSystem.PopupEntity(notEnoughReagentMessage, args.User, args.User, PopupType.Large);
            _delay.TryResetDelay((ent, useDelay));
            return;
        }

        _solutionContainer.RemoveReagent(solution.Value, ent.Comp.Reagent, ent.Comp.Consumption);

        var damage = _damageableSystem.TryChangeDamage(args.Target!.Value, ent.Comp.Damage, true, origin: ent);

        if (damage == null || damage.Empty)
        {
            var othersMessage = Loc.GetString("censer-heal-success-none-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)));
            _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

            var selfMessage = Loc.GetString("censer-heal-success-none-self", ("target", Identity.Entity(args.Target.Value, EntityManager)));
            _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
        }
        else
        {
            var othersMessage = Loc.GetString("censer-heal-success-others", ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(args.Target.Value, EntityManager)));
            _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);

            var selfMessage = Loc.GetString("censer-heal-success-self", ("target", Identity.Entity(args.Target.Value, EntityManager)));
            _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
            _delay.TryResetDelay((ent, useDelay));
        }
    }

    private void OnAfterInteract(Entity<CenserComponent> uid, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null || args.Target == args.User || !_mobStateSystem.IsAlive(args.Target.Value))
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _delay.IsDelayed((uid, useDelay)))
            return;

        if (!HasComp<BibleUserComponent>(args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("censer-sizzle"), args.User, args.User);
            _delay.TryResetDelay((uid, useDelay));

            return;
        }

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.UsingDelay, new CenserDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (doAfterCancelled)
        {
            return;
        }
    }
}
using Content.Server.Animals.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Udder;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
///     Gives ability to produce milkable reagents, produces endless if the
///     owner has no HungerComponent
/// </summary>
internal sealed class UdderSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UdderComponent, GetVerbsEvent<AlternativeVerb>>(AddMilkVerb);
        SubscribeLocalEvent<UdderComponent, MilkingDoAfterEvent>(OnDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<UdderComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var udder))
        {
            if (now < udder.NextGrowth)
                continue;

            udder.NextGrowth = now + udder.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (EntityManager.TryGetComponent(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -udder.HungerUsage, hunger);
            }

            if (!_solutionContainerSystem.ResolveSolution(uid, udder.SolutionName, ref udder.Solution))
                continue;

            //TODO: toxins from bloodstream !?
            _solutionContainerSystem.TryAddReagent(udder.Solution.Value, udder.ReagentId, udder.QuantityPerUpdate, out _);
        }
    }

    private void AttemptMilk(Entity<UdderComponent?> udder, EntityUid userUid, EntityUid containerUid)
    {
        if (!Resolve(udder, ref udder.Comp))
            return;

        var doargs = new DoAfterArgs(EntityManager, userUid, 5, new MilkingDoAfterEvent(), udder, udder, used: containerUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        _doAfterSystem.TryStartDoAfter(doargs);
    }

    private void OnDoAfter(Entity<UdderComponent> entity, ref MilkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        if (!_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        if (!_solutionContainerSystem.TryGetRefillableSolution(args.Args.Used.Value, out var targetSoln, out var targetSolution))
            return;

        args.Handled = true;
        var quantity = solution.Volume;
        if (quantity == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("udder-system-dry"), entity.Owner, args.Args.User);
            return;
        }

        if (quantity > targetSolution.AvailableVolume)
            quantity = targetSolution.AvailableVolume;

        var split = _solutionContainerSystem.SplitSolution(entity.Comp.Solution.Value, quantity);
        _solutionContainerSystem.TryAddSolution(targetSoln.Value, split);

        _popupSystem.PopupEntity(Loc.GetString("udder-system-success", ("amount", quantity), ("target", Identity.Entity(args.Args.Used.Value, EntityManager))), entity.Owner,
            args.Args.User, PopupType.Medium);
    }

    private void AddMilkVerb(Entity<UdderComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using == null ||
             !args.CanInteract ||
             !EntityManager.HasComponent<RefillableSolutionComponent>(args.Using.Value))
            return;

        var uid = entity.Owner;
        var user = args.User;
        var used = args.Using.Value;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptMilk(uid, user, used);
            },
            Text = Loc.GetString("udder-system-verb-milk"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }
}

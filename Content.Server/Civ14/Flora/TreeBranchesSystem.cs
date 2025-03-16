using Content.Server.DoAfter;
using Content.Shared.Flora;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Popups;

namespace Content.Server.Flora;

public sealed partial class TreeBranchesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom Random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TreeBranchesComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TreeBranchesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TreeBranchesComponent, CollectBranchDoAfterEvent>(OnDoAfter);
    }

    private void OnMapInit(EntityUid uid, TreeBranchesComponent component, MapInitEvent args)
    {
        component.CurrentBranches = Random.Next(0, component.MaxBranches);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TreeBranchesComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var currentTime = _gameTiming.CurTime;
            if (currentTime >= component.LastGrowthTime + TimeSpan.FromSeconds(component.GrowthTime))
            {
                if (component.CurrentBranches < component.MaxBranches)
                {
                    component.CurrentBranches++;
                    component.LastGrowthTime = currentTime;
                }
            }
        }
    }

    private void OnInteractHand(EntityUid uid, TreeBranchesComponent component, InteractHandEvent args)
    {
        if (args.Handled || component.CurrentBranches <= 0)
        {
            _popup.PopupEntity("There are no branches left to collect.", uid, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            component.CollectionTime,
            new CollectBranchDoAfterEvent(),
            uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, TreeBranchesComponent component, ref CollectBranchDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        component.CurrentBranches--;
        var spawnPos = Transform(uid).MapPosition;
        Spawn("BranchItem", spawnPos);
        _popup.PopupEntity("You successfully collect a branch.", uid, args.Args.User);
        args.Handled = true;
    }
}
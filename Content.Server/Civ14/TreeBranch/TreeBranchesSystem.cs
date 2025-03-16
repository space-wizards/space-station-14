using Content.Server.DoAfter;
using Content.Shared.TreeBranch;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;

namespace Content.Server.TreeBranch;

public sealed partial class TreeBranchesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom Random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TreeBranchesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TreeBranchesComponent, CollectBranchDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<TreeBranchesComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
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

private void OnGetVerbs(EntityUid uid, TreeBranchesComponent component, ref GetVerbsEvent<AlternativeVerb> args)
{
    if (!args.CanAccess || !args.CanInteract || component.CurrentBranches <= 0)
        return;

    var user = args.User;

    var verb = new AlternativeVerb
    {
        Text = Loc.GetString("collect-branch-verb"),
        Act = () => StartCollectingBranch(uid, component, user)
    };
    args.Verbs.Add(verb);
}


    private void StartCollectingBranch(EntityUid treeUid, TreeBranchesComponent component, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            component.CollectionTime,
            new CollectBranchDoAfterEvent(),
            treeUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, TreeBranchesComponent component, ref CollectBranchDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        component.CurrentBranches--;
        var spawnPos = Transform(uid).MapPosition;
        var branch = Spawn("LeafedStick", spawnPos);
        _hands.TryPickupAnyHand(args.Args.User, branch);
        _popup.PopupEntity("You successfully collect a branch.", uid, args.Args.User);
        args.Handled = true;
    }
}
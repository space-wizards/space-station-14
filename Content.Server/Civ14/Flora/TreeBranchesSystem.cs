using Content.Shared.Flora;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Flora;

public sealed partial class TreeBranchesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom Random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Listens for interactions with empty hands
        SubscribeLocalEvent<TreeBranchesComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<TreeBranchesComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, TreeBranchesComponent component, MapInitEvent args)
    {
        // Randomizes the number of branches between 0 and the defined maximum (inclusive).
        component.CurrentBranches = Random.Next(0, component.MaxBranches);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TreeBranchesComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var currentTime = _gameTiming.CurTime;

            // Checks if it is time for a new branch to grow
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
        if (args.Handled)
            return;

        // Decreases the number of branches
        component.CurrentBranches--;

        // Spawns the "BranchItem" based on probability
        if (component.SpawnProbability >= 1f || Random.Prob(component.SpawnProbability))
        {
            var spawnPos = Transform(uid).MapPosition;
            Spawn("BranchItem", spawnPos); // Creates the item at the tree's position
        }

        // Marks the event as handled
        args.Handled = true;
    }
}

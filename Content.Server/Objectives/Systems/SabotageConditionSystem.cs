using Content.Server.Objectives.Components;
using Content.Shared.Traitor;
using Content.Shared.Traitor.Components;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Systems;

public sealed partial class SabotageConditionSystem : EntitySystem
{
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeaconHackableComponent, StructureHackedEvent>(OnHack);
        SubscribeLocalEvent<BeaconHackableComponent, StructureHackCompletedEvent>(OnHackFinish);
    }

    private void HandleSabotage(Entity<BeaconHackableComponent> ent, bool confirmationCompletion = false)
    {
        var query = EntityQueryEnumerator<SabotageConditionComponent>();
        while (query.MoveNext(out var uid, out var sabotageCondition))
        {
            if (!TryComp<CodeConditionComponent>(uid, out var codeCondition))
                continue;
            if (_whitelist.IsWhitelistPass(sabotageCondition.Whitelist, ent) && sabotageCondition.RequireConfirmation == confirmationCompletion)
                _codeCondition.SetCompleted((uid, codeCondition));
        }
    }

    private void OnHack(Entity<BeaconHackableComponent> ent, ref StructureHackedEvent args) // Ran upon initial planting.
    {
        HandleSabotage(ent);
    }

    private void OnHackFinish(Entity<BeaconHackableComponent> ent, ref StructureHackCompletedEvent args) // Ran upon confirmation from another system.
    {
        HandleSabotage(ent, true);
    }
}

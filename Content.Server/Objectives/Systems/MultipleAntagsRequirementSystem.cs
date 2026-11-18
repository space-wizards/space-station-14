using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Objectives.Components;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// Handles requiring multiple traitors being alive for the objective to be given.
/// </summary>
public sealed partial class MultipleAntagsRequirementSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antagSelection = default!;

    [SubscribeLocalEvent]
    private void OnCheck(EntityUid uid, MultipleAntagsRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        var count = 0;
        var query = EntityQueryEnumerator<AntagSelectionComponent>(); // there is no root singleton that holds gamerules (or gamerule entities), so this is necessary
        while (query.MoveNext(out var gamerule, out var antagSelectionComp))
        {
            foreach (var antagProto in comp.Antags)
            {
                count += _antagSelection.GetAssignedAntagCount((gamerule, antagSelectionComp), antagProto);
            }
        }
        if (count < comp.Count) // does not exclude the person asking.
            args.Cancelled = true;
    }
}

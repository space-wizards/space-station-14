using Content.Server.Antag;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Systems;

public sealed class SubmissionObjectiveSystem : EntitySystem
{

    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    /// <summary>
    /// Gives a player a objective to obey another player
    /// </summary>
    /// <param name="player">The target we give this objective to</param>
    /// <param name="mindUid">The player's mind</param>
    /// <param name="masterUid">The master's entity that the player will obey</param>
    /// <param name="objective">The objective that we give the player</param>
    /// <returns></returns>
    public bool MakeMinion(EntityUid player, Entity<MindComponent> mindUid, EntityUid? masterUid, EntProtoId objective)
    {
        if (masterUid == null)
            return false;

        var targetComp = EnsureComp<TargetOverrideComponent>(player);

        _mindSystem.TryGetMind(masterUid.Value, out var masterMind, out var _);

        var masterName = "Unknown";
        if (TryComp<MindComponent>(masterMind, out var masterMindComp) && masterMindComp != null && masterMindComp.CharacterName != null)
            masterName = masterMindComp.CharacterName;

        targetComp.Target = masterMind;

        if (_mindSystem.TryAddObjective(mindUid, mindUid.Comp, objective))
        {
            _antag.SendBriefing(player, Loc.GetString("objective-start-minion-submission", ("targetName", masterName)), null, null);
            return true;
        }

        return false;
    }
}

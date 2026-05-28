using System.Linq;
using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class ChangelingEscapeIdentityConditionSystem : EntitySystem
{
    [Dependency] private EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingEscapeIdentityConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<ChangelingEscapeIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(ent, out var target))
            return;

        args.Progress = GetProgress((args.MindId, args.Mind), target.Value);
    }

    private float GetProgress(Entity<MindComponent> mind, EntityUid targetMind)
    {
        if (mind.Comp.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 1f;

        // Check 1: Must have transformed to the target person.
        if (!TryComp<ChangelingIdentityComponent>(mind.Comp.OwnedEntity, out var identityComp))
            return 0f;

        if (!TryComp<MindComponent>(targetMind, out var targetMindComp))
            return 0f;

        var currentData = identityComp.ConsumedIdentities.FirstOrDefault(d => d.Identity == identityComp.CurrentIdentity);
        if (currentData == null || currentData.OriginalMind != targetMind)
            return 0f;

        // Check 2: Must escape alive.
        if (!_emergencyShuttle.IsTargetEscaping(mind.Comp.OwnedEntity.Value))
            return 0.5f;

        // Check 3: Must wear an ID card with the target's name on it.
        if (!_idCard.TryFindIdCard(mind.Comp.OwnedEntity.Value, out var idCard))
            return 0.75f;

        if (idCard.Comp.FullName != targetMindComp.CharacterName)
            return 0.75f;

        return 1f;
    }
}

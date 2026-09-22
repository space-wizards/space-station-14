using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Changeling.Systems;
using Content.Shared.Cuffs;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class ChangelingEscapeIdentityConditionSystem : EntitySystem
{
    [Dependency] private EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private SharedChangelingIdentitySystem _changelingIdentity = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingEscapeIdentityConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ChangelingEscapeIdentityConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(Entity<ChangelingEscapeIdentityConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(ent, out var target))
            return;

        if (!TryComp<MindComponent>(target.Value, out var targetMind))
            return;

        ent.Comp.TargetName = targetMind.CharacterName;
    }

    private void OnGetProgress(Entity<ChangelingEscapeIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent, (args.MindId, args.Mind));
    }

    private float GetProgress(Entity<ChangelingEscapeIdentityConditionComponent> ent, Entity<MindComponent> mind)
    {
        var ownedEntity = mind.Comp.OwnedEntity;
        if (ownedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // Check 1: Must have transformed to the target person.
        if (!_changelingIdentity.TryGetCurrentIdentityData(ownedEntity.Value, out var identityData)
            || identityData.OriginalName != ent.Comp.TargetName)
            return 0f;

        // Check 2: Must escape alive.
        if (!_emergencyShuttle.IsTargetEscaping(ownedEntity.Value))
            return 0.5f;

        if (_cuffable.IsCuffed(ownedEntity.Value))
            return 0.5f;

        // Check 3: Must wear an ID card with the target's name on it.
        if (!_idCard.TryFindIdCard(ownedEntity.Value, out var idCard))
            return 0.75f;

        if (idCard.Comp.FullName != ent.Comp.TargetName)
            return 0.75f;

        return 1f;
    }
}

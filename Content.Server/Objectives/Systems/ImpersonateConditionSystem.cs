using Content.Server.Objectives.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class ImpersonateConditionSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImpersonateConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<ImpersonateConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<ImpersonateConditionComponent>();
        var time = TimeSpan.FromSeconds(deltaTime);
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Name == null || comp.MindId == null)
                continue;

            if (!TryComp<MindComponent>(comp.MindId, out var mind) || mind.OwnedEntity == null)
                continue;

            // increase impersonated time until its complete
            if (Identity.Name(mind.OwnedEntity.Value, EntityManager) == comp.Name)
                comp.TimeImpersonated += time;
            // decrease it until its gone
            else
                comp.TimeImpersonated -= time;

            // clamp it
            if (comp.TimeImpersonated < TimeSpan.Zero)
                comp.TimeImpersonated = TimeSpan.Zero;
            if (comp.TimeImpersonated > comp.Duration)
                comp.TimeImpersonated = comp.Duration;
        }
    }

    private void OnAfterAssign(EntityUid uid, ImpersonateConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        if (!TryComp<MindComponent>(target, out var targetMind) || targetMind.CharacterName == null)
            return;

        comp.Name = targetMind.CharacterName;
        comp.MindId = args.MindId;
    }

    private void OnGetProgress(EntityUid uid, ImpersonateConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = (float) (comp.TimeImpersonated / comp.Duration);
    }
}

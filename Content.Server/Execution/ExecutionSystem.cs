using System.Numerics;
using Content.Server.Chat;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Execution;
using Content.Shared.Mind;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Execution;

public sealed class ExecutionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedExecutionSystem _execution = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SuicideSystem _suicide = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecutionComponent, ExecutionDoAfterEvent>(OnExecutionDoAfter);
    }

    private void OnExecutionDoAfter(Entity<ExecutionComponent> entity, ref ExecutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
            return;

        if (!TryComp<MeleeWeaponComponent>(entity, out var meleeWeaponComp))
            return;

        var attacker = args.User;
        var victim = args.Target.Value;
        var weapon = args.Used.Value;

        if (!_execution.CanBeExecuted(victim, attacker))
            return;

        // This is needed so the melee system does not stop it.
        var prev = _combat.IsInCombatMode(attacker);
        _combat.SetInCombatMode(attacker, true);
        entity.Comp.Executing = true;
        string? internalMsg = null;
        string? externalMsg = null;

        internalMsg = entity.Comp.CompleteInternalMeleeExecutionMessage;
        externalMsg = entity.Comp.CompleteExternalMeleeExecutionMessage;

        var userXform = Transform(attacker);

        if (attacker == victim)
        {
            Spawn(meleeWeaponComp.Animation, userXform.Coordinates);
            if (_mind.TryGetMind(victim, out var mindId, out var mindComp))
                _suicide.Suicide(victim, (mindId, mindComp));
        }
        else
        {
            _melee.AttemptLightAttack(attacker, weapon, meleeWeaponComp, victim);
            var targetPos = _transform.GetWorldPosition(victim);
            var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
            localPos = userXform.LocalRotation.RotateVec(localPos);

            if (localPos.LengthSquared() <= 0f)
                return;

            const float bufferLength = 0.2f;
            var visualLength = meleeWeaponComp.Range - bufferLength;

            if (localPos.Length() > visualLength)
                localPos = localPos.Normalized() * visualLength;

            _melee.DoLunge(attacker, weapon, meleeWeaponComp.Angle, localPos, meleeWeaponComp.Animation, false);
            _audio.PlayEntity(meleeWeaponComp.HitSound, attacker, victim);
        }

        _combat.SetInCombatMode(attacker, prev);
        entity.Comp.Executing = false;
        args.Handled = true;

        if (attacker != victim)
        {
            _execution.ShowExecutionInternalPopup(internalMsg, attacker, victim, entity, false);
            _execution.ShowExecutionExternalPopup(externalMsg, attacker, victim, entity);
        }
    }
}

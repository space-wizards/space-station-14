using System.Numerics;
using Content.Server.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Execution;
using Content.Shared.Mind;
using Content.Shared.Weapons.Melee;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Execution
{
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
            SubscribeLocalEvent<ExecutionComponent, SuicideEvent>(OnSuicide);
        }

        private void OnExecutionDoAfter(EntityUid uid, ExecutionComponent component, ExecutionDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Used == null || args.Target == null)
                return;

            var attacker = args.User;
            var victim = args.Target.Value;
            var weapon = args.Used.Value;

            if (!_execution.CanBeExecuted(victim, attacker))
                return;

            // This is needed so the melee system does not stop it.
            var prev = _combat.IsInCombatMode(attacker);
            _combat.SetInCombatMode(attacker, true);
            component.Executing = true;
            string? internalMsg = null;
            string? externalMsg = null;

            if (TryComp(uid, out MeleeWeaponComponent? melee))
            {
                internalMsg = component.DefaultCompleteInternalMeleeExecutionMessage;
                externalMsg = component.DefaultCompleteExternalMeleeExecutionMessage;

                var userXform = Transform(attacker);

                if (attacker == victim)
                {
                    Spawn(melee.Animation, userXform.Coordinates);
                    if (_mind.TryGetMind(victim, out var mindId, out var mind))
                        _suicide.Suicide(victim, mindId, mind: mind);
                }
                else
                {
                    _melee.AttemptLightAttack(attacker, weapon, melee, victim);
                    var targetPos = _transform.GetWorldPosition(victim);
                    var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
                    localPos = userXform.LocalRotation.RotateVec(localPos);

                    if (localPos.LengthSquared() <= 0f)
                        return;

                    const float bufferLength = 0.2f;
                    var visualLength = melee.Range - bufferLength;

                    if (localPos.Length() > visualLength)
                        localPos = localPos.Normalized() * visualLength;

                    _melee.DoLunge(attacker, weapon, melee.Angle, localPos, melee.Animation, false);
                }
                _audio.PlayEntity(melee.HitSound, attacker, victim);
            }

            _combat.SetInCombatMode(attacker, prev);
            component.Executing = false;
            args.Handled = true;

            if (internalMsg != null && externalMsg != null)
            {
                _execution.ShowExecutionInternalPopup(internalMsg, attacker, victim, uid, false);
                _execution.ShowExecutionExternalPopup(externalMsg, attacker, victim, uid);
            }
        }

        private void OnSuicide(EntityUid uid, ExecutionComponent comp, ref SuicideEvent args)
        {
            if (!TryComp<MeleeWeaponComponent>(uid, out var melee))
                return;

            args.Damage = melee.Damage;
            args.Handled = true;
        }
    }
}

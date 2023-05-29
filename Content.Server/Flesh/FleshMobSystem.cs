using Content.Server.Popups;
using Content.Shared.Flesh;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Map;

namespace Content.Server.Flesh
{
    public sealed class FleshMobSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FleshMobComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<FleshMobComponent, AttackAttemptEvent>(OnAttackAttempt);

        }

        private void OnAttackAttempt(EntityUid uid, FleshMobComponent component, AttackAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (HasComp<FleshMobComponent>(args.Target))
            {
                _popup.PopupCursor(Loc.GetString("flesh-mob-cant-atack-flesh-mob"), uid,
                    PopupType.LargeCaution);
                args.Cancel();
            }
            if (HasComp<FleshCultistComponent>(args.Target))
            {
                _popup.PopupCursor(Loc.GetString("flesh-mob-cant-atack-flesh-cultist"), uid,
                    PopupType.LargeCaution);
                args.Cancel();
            }

            if (HasComp<FleshHeartComponent>(args.Target))
            {
                _popup.PopupCursor(Loc.GetString("flesh-mob-cant-atack-flesh-heart"), uid,
                    PopupType.LargeCaution);
                args.Cancel();
            }
        }

        private void OnMobStateChanged(EntityUid uid, FleshMobComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState != MobState.Dead)
                return;
            if (component.SoundDeath == null)
                return;
            _audioSystem.PlayPvs(component.SoundDeath, uid, component.SoundDeath.Params);

            var random = new Random();
            var spawnDistance = 0.25f;

            for (int i = 0; i < component.DeathMobSpawnCount; i++)
            {
                var offsetX = (float)(random.NextDouble() * spawnDistance * 2 - spawnDistance);
                var offsetY = (float)(random.NextDouble() * spawnDistance * 2 - spawnDistance);
                var coords = new EntityCoordinates(uid, new Vector2(offsetX, offsetY));
                Spawn(component.DeathMobSpawnId, coords);
            }
        }
    }
}


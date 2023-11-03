
using Robust.Server.GameObjects;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Pregant.Components;

namespace Content.Server.Pregant
{
    public sealed class PregantSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly TransformSystem _xform = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PregantComponent, MobStateChangedEvent>(OnDamage);
        }


        private void OnDamage(EntityUid uid, PregantComponent component, MobStateChangedEvent args)
        {
            if (_mobState.IsDead(uid))
                {
                    _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid, AudioParams.Default.WithVariation(0.2f).WithVolume(1f));
                    Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates);
                }

        }


    }
}

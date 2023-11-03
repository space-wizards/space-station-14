
using Robust.Server.GameObjects;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.InfectionDead.Components;

namespace Content.Server.InfectionDead
{
    public sealed class InfectionDeadSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly TransformSystem _xform = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InfectionDeadComponent, InfectionDeadDamageEvent>(GetDamage);
            SubscribeLocalEvent<InfectionDeadComponent, MobStateChangedEvent>(OnState);
        }

        private void GetDamage(EntityUid uid, InfectionDeadComponent component, ref InfectionDeadDamageEvent args)
        {
            if (_mobState.IsDead(uid))
                {return;}
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add("Cellular", 15f);
            _damage.TryChangeDamage(uid, dspec, true, false);
        }

        private void OnState(EntityUid uid, InfectionDeadComponent component, MobStateChangedEvent args)
        {
            if (_mobState.IsDead(uid))
                {
                   // DamageSpecifier dspec = new();
                   // dspec.DamageDict.Add("Slash", 200f);
                   // _damage.TryChangeDamage(uid, dspec, true, false);
                    _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid, AudioParams.Default.WithVariation(0.2f).WithVolume(-4f));
                    _bodySystem.GibBody(uid);
                    Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates);
                }
        }


    }
}

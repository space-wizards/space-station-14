using Content.Server.Disease;
using Content.Server.Disease.Components;

namespace Content.Server.Weapon.Melee.ZombieHand
{
    public sealed class ZombieHandSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseZombieSystem _diseaseZombie = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ZombieHandComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, ZombieHandComponent component, MeleeHitEvent args)
        {
            if (EntityManager.TryGetComponent<DiseaseZombieComponent>(args.User, out var comp))
            {
                _diseaseZombie.OnMeleeHit(uid, comp, args);
            }
        }
    }
}

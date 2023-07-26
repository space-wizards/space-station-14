using Content.Shared.Damage;

namespace Content.Server.Blob
{
    public sealed class BlobMobSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BlobMobComponent, BlobMobGetPulseEvent>(OnPulsed);
        }

        private void OnPulsed(EntityUid uid, BlobMobComponent component, BlobMobGetPulseEvent args)
        {
            _damageableSystem.TryChangeDamage(uid, component.HealthOfPulse);
        }
    }
}

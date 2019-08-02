using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    [RegisterComponent]
    public class BallisticBulletComponent : Component
    {
        public override string Name => "BallisticBullet";

        private BallisticCaliber _caliber;
        private string _projectileType;
        private bool _spent;

        public string ProjectileType => _projectileType;
        public BallisticCaliber Caliber => _caliber;
        public bool Spent
        {
            get => _spent;
            set => _spent = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _projectileType, "projectile", null);
            serializer.DataField(ref _spent, "spent", false);
        }
    }
}

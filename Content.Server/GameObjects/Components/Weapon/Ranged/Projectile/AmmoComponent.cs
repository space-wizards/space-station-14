using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    /// <summary>
    ///     Passes information about the projectiles to be fired by AmmoWeapons
    /// </summary>
    [RegisterComponent]
    public class AmmoComponent : Component
    {
        public override string Name => "BallisticBullet";

        /// <summary>
        ///     Cartridge calibre, restricts what AmmoWeapons this ammo can be fired from.
        /// </summary>
        private BallisticCaliber _caliber;
        public BallisticCaliber Caliber => _caliber;

        /// <summary>
        ///     YAML ID of the projectiles to be created when firing this ammo.
        /// </summary>
        private string _projectileID;
        public string ProjectileID => _projectileID;

        /// <summary>
        ///     How many copies of the projectile are shot.
        /// </summary>
        private int _projectilesFired;
        public int ProjectilesFired => _projectilesFired;

        /// <summary>
        ///     Weapons that fire projectiles from ammo types.
        /// </summary>
        private float _spreadStdDev_Ammo;
        public float SpreadStdDev_Ammo => _spreadStdDev_Ammo;

        /// <summary>
        ///     Arc angle of shotgun pellet spreads, only used if multiple projectiles are being fired.
        /// </summary>
        private float _evenSpreadAngle_Ammo;
        public float EvenSpreadAngle_Ammo => _evenSpreadAngle_Ammo;

        /// <summary>
        ///     Adds additional velocity to the projectile, on top of what it already has.
        /// </summary>
        private float _velocity_Ammo;
        public float Velocity_Ammo => _velocity_Ammo;

        /// <summary>
        ///     If the ammo cartridge has been shot already.
        /// </summary>
        private bool _spent;
        public bool Spent
        {
            get => _spent;
            set => _spent = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _projectileID, "projectile", null);
            serializer.DataField(ref _spent, "spent", false);
            serializer.DataField(ref _projectilesFired, "projectilesfired", 1);
            serializer.DataField(ref _spreadStdDev_Ammo, "ammostddev", 0);
            serializer.DataField(ref _evenSpreadAngle_Ammo, "ammospread", 0);
            serializer.DataField(ref _velocity_Ammo, "ammovelocity", 0);
        }
    }
    public enum BallisticCaliber
    {
        Unspecified = 0,
        // .32
        A32,
        // .357
        A357,
        // .44
        A44,
        // .45mm
        A45mm,
        // .50 cal
        A50,
        // 5.56mm
        A556mm,
        // 6.5mm
        A65mm,
        // 7.62mm
        A762mm,
        // 9mm
        A9mm,
        // 10mm
        A10mm,
        // 20mm
        A20mm,
        // 24mm
        A24mm,
        // 12g
        A12g,
    }
}

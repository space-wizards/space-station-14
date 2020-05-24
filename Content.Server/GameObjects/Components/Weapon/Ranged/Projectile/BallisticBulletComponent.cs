using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Projectile
{
    /// <summary>
    ///     Passes information about the projectiles to be fired by AmmoWeapons
    /// </summary>
    [RegisterComponent]
    public class BallisticBulletComponent : Component
    {
        public override string Name => "BallisticBullet";

        private BallisticCaliber _caliber;
        /// <summary>
        ///     Cartridge calibre, restricts what AmmoWeapons this ammo can be fired from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public BallisticCaliber Caliber { get => _caliber; set => _caliber = value; }

        private string _projectileID;
        /// <summary>
        ///     YAML ID of the projectiles to be created when firing this ammo.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string ProjectileID { get => _projectileID; set => _projectileID = value; }

        private int _projectilesFired;
        /// <summary>
        ///     How many copies of the projectile are shot.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int ProjectilesFired { get => _projectilesFired; set => _projectilesFired = value; }

        private float _spreadStdDev_Ammo;
        /// <summary>
        ///     Weapons that fire projectiles from ammo types.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpreadStdDev_Ammo { get => _spreadStdDev_Ammo; set => _spreadStdDev_Ammo = value; }

        private float _evenSpreadAngle_Ammo;
        /// <summary>
        ///     Arc angle of shotgun pellet spreads, only used if multiple projectiles are being fired.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float EvenSpreadAngle_Ammo { get => _evenSpreadAngle_Ammo; set => _evenSpreadAngle_Ammo = value; }

        private float _velocity_Ammo;
        /// <summary>
        ///     Adds additional velocity to the projectile, on top of what it already has.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Velocity_Ammo { get => _velocity_Ammo; set => _velocity_Ammo = value; }

        private bool _spent;
        /// <summary>
        ///     If the ammo cartridge has been shot already.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Spent { get => _spent; set => _spent = value; }

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

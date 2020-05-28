using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Attachments
{
    [RegisterComponent]
    public sealed class AttachmentMuzzleComponent: Component
    {
        public override string Name => "AttachmentMuzzle";

        public AttachmentSlot Slot => AttachmentSlot.Muzzle;

        public string SoundGunshot => _soundGunshot;
        private string _soundGunshot;

        private string _originalSound;
        
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _soundGunshot, "sound_gunshot", "/Audio/Guns/Gunshots/silenced.ogg");
        }

        public void Attached()
        {
            var rangedWeapon = Owner.GetComponent<ServerRangedWeaponComponent>();
            _originalSound = rangedWeapon.SoundGunshot;
            rangedWeapon.SetGunshotSound(SoundGunshot);
        }

        public void Detached()
        {
            var rangedWeapon = Owner.GetComponent<ServerRangedWeaponComponent>();
            rangedWeapon.SetGunshotSound(_originalSound);
            _originalSound = null;
        }
    }
}
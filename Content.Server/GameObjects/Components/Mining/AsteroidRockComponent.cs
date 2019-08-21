using System;
using System.Runtime.InteropServices;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Mining
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IAttackBy
    {
        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

        public override void Initialize()
        {
            base.Initialize();
            var spriteComponent = Owner.GetComponent<SpriteComponent>();
            var random = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
            spriteComponent.LayerSetState(0, random.Pick(SpriteStates));
        }

        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            var item = eventArgs.AttackWith;
            if (!item.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent)) return false;

            Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Brute, meleeWeaponComponent.Damage);

            if (!item.TryGetComponent(out PickaxeComponent pickaxeComponent)) return true;
            if (!string.IsNullOrWhiteSpace(pickaxeComponent.MiningSound) &&
                item.TryGetComponent<SoundComponent>(out var soundComponent))
            {
                soundComponent.Play(pickaxeComponent.MiningSound, AudioParams.Default);
            }
            return true;
        }
    }
}

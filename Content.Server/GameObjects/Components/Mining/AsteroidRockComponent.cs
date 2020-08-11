using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Mining
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IInteractUsing
    {
        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();
            var spriteComponent = Owner.GetComponent<SpriteComponent>();
            spriteComponent.LayerSetState(0, _random.Pick(SpriteStates));
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!item.TryGetComponent(out MeleeWeaponComponent meleeWeaponComponent)) return false;

            Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Brute, meleeWeaponComponent.Damage, item, eventArgs.User);

            if (!item.TryGetComponent(out PickaxeComponent pickaxeComponent)) return true;
            if (!string.IsNullOrWhiteSpace(pickaxeComponent.MiningSound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(pickaxeComponent.MiningSound, Owner, AudioParams.Default);
            }
            return true;
        }
    }
}

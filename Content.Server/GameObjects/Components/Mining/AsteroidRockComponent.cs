using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Mining
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

        public override void Initialize()
        {
            base.Initialize();

            var spriteComponent = Owner.EnsureComponent<SpriteComponent>();
            spriteComponent.LayerSetState(0, _random.Pick(SpriteStates));
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!item.TryGetComponent(out MeleeWeaponComponent? meleeWeaponComponent)) return false;

            Owner.GetComponent<IDamageableComponent>().ChangeDamage(DamageType.Blunt, meleeWeaponComponent.Damage, false, item);

            if (!item.TryGetComponent(out PickaxeComponent? pickaxeComponent)) return true;
            if (!string.IsNullOrWhiteSpace(pickaxeComponent.MiningSound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), pickaxeComponent.MiningSound, Owner, AudioParams.Default);
            }
            return true;
        }
    }
}

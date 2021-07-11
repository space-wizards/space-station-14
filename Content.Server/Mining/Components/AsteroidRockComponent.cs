using System.Threading.Tasks;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Mining;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

        [DataField("damageType",required: true)]
        private readonly DamageTypePrototype _damageType = default!;

        protected override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(AsteroidRockVisuals.State, _random.Pick(SpriteStates));
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!item.TryGetComponent(out MeleeWeaponComponent? meleeWeaponComponent)) return false;

            var DamageableComponent = Owner.GetComponent<IDamageableComponent>().ChangeDamage(
                _damageType, meleeWeaponComponent.Damage, false, item);

            if (!item.TryGetComponent(out PickaxeComponent? pickaxeComponent)) return true;
            if (!string.IsNullOrWhiteSpace(pickaxeComponent.MiningSound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), pickaxeComponent.MiningSound, Owner, AudioParams.Default);
            }
            return true;
        }
    }
}

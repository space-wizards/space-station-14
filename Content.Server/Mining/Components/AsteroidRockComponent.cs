using System.Threading.Tasks;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mining;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

        protected override void Initialize()
        {
            base.Initialize();
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(AsteroidRockVisuals.State, _random.Pick(SpriteStates));
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!_entMan.TryGetComponent(item, out MeleeWeaponComponent? meleeWeaponComponent))
                return false;

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(Owner, meleeWeaponComponent.Damage);

            if (!_entMan.TryGetComponent(item, out PickaxeComponent? pickaxeComponent))
                return true;

            SoundSystem.Play(Filter.Pvs(Owner), pickaxeComponent.MiningSound.GetSound(), Owner, AudioParams.Default);
            return true;
        }
    }
}

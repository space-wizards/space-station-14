using System.Threading.Tasks;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Mining;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
=======
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
>>>>>>> refactor-damageablecomponent

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class AsteroidRockComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "AsteroidRock";
        private static readonly string[] SpriteStates = {"0", "1", "2", "3", "4"};

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
=======
=======
>>>>>>> refactor-damageablecomponent
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt"!;
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;

<<<<<<< HEAD
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
        protected override void Initialize()
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(AsteroidRockVisuals.State, _random.Pick(SpriteStates));
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!item.TryGetComponent(out MeleeWeaponComponent? meleeWeaponComponent))
                return false;

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            Owner.GetComponent<IDamageableComponent>().ChangeDamage(DamageType.Blunt, meleeWeaponComponent.Damage, false, item);
=======
            Owner.GetComponent<IDamageableComponent>().TryChangeDamage(DamageType, meleeWeaponComponent.Damage);
>>>>>>> Refactor damageablecomponent update (#4406)
=======
            Owner.GetComponent<IDamageableComponent>().TryChangeDamage(DamageType, meleeWeaponComponent.Damage);
>>>>>>> refactor-damageablecomponent

            if (!item.TryGetComponent(out PickaxeComponent? pickaxeComponent))
                return true;

            SoundSystem.Play(Filter.Pvs(Owner), pickaxeComponent.MiningSound.GetSound(), Owner, AudioParams.Default);
            return true;
        }
    }
}

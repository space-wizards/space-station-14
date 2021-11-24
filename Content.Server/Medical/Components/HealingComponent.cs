using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public class HealingComponent : Component, IAfterInteract
    {
        public override string Name => "Healing";

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        /// <remarks>
        ///     The supported damage types are specified using a <see cref="DamageContainerPrototype"/>s. For a
        ///     HealingComponent this filters what damage container type this component should work on. If null,
        ///     all damage container types are supported.
        /// </remarks>
        [DataField("damageContainer", customTypeSerializer: typeof(PrototypeIdSerializer<DamageContainerPrototype>))]
        public string? DamageContainerID;

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (!eventArgs.Target.TryGetComponent<DamageableComponent>(out DamageableComponent? targetDamage))
            {
                return true;
            }
            else if (DamageContainerID is not null && !DamageContainerID.Equals(targetDamage.DamageContainerID))
            {
                return true;
            }

            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User.Uid))
            {
                return true;
            }

            if (eventArgs.User != eventArgs.Target &&
                !eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                return true;
            }

            if (Owner.TryGetComponent<SharedStackComponent>(out var stack) && !EntitySystem.Get<StackSystem>().Use(Owner.Uid, 1, stack))
            {
                return true;
            }

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(eventArgs.Target.Uid, Damage, true);

            return true;
        }
    }
}

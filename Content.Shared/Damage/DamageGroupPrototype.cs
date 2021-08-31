using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage
{
    /// <summary>
    ///     A Group of <see cref="DamageTypePrototype"/>s.
    /// </summary>
    /// <remarks>
    ///     These groups can be used to specify supported damage types of a <see
    ///     cref="Container.DamageContainerPrototype"/>, or to change/get/set damage in a <see
    ///     cref="Components.DamageableComponent"/>.
    /// </remarks>
    [Prototype("damageGroup")]
    [Serializable, NetSerializable]
    public class DamageGroupPrototype : IPrototype, ISerializationHooks
    {
        private IPrototypeManager _prototypeManager = default!;

        [DataField("id", required: true)] public string ID { get; } = default!;

        [DataField("damageTypes", required: true)]
        public List<string> TypeIDs { get; } = default!;

        public HashSet<DamageTypePrototype> DamageTypes { get; } = new();

        // Create set of damage types
        void ISerializationHooks.AfterDeserialization()
        {
            _prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var typeID in TypeIDs)
            {
                DamageTypes.Add(_prototypeManager.Index<DamageTypePrototype>(typeID));
            }
        }
    }
}

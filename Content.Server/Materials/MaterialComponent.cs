using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.ViewVariables;

namespace Content.Server.Materials
{
    /// <summary>
    ///     Component to store data such as "this object is made out of steel".
    ///     This is not a storage system for say smelteries.
    /// </summary>
    [RegisterComponent]
    public class MaterialComponent : Component
    {
        [ViewVariables]
        [DataField("materials", customTypeSerializer:typeof(PrototypeIdListSerializer<MaterialPrototype>))]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly List<string> _materials = new();
        public IEnumerable<string> MaterialIds => _materials;

        /// <summary>
        ///     Returns all materials which make up this entity.
        ///     This property has an IoC resolve and is generally slow, so be sure to cache the results if needed.
        /// </summary>
        [ViewVariables]
        public IEnumerable<MaterialPrototype> Materials
        {
            get
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                foreach (var id in MaterialIds)
                {
                    if(prototypeManager.TryIndex<MaterialPrototype>(id, out var material))
                        yield return material;
                    else
                        Logger.Error($"Material prototype {id} does not exist! Entity: {Owner}");
                }
            }
        }
    }
}

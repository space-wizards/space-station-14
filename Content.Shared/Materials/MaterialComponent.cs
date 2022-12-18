using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Materials
{
    /// <summary>
    ///     Component to store data such as "this object is made out of steel".
    ///     This is not a storage system for say smelteries.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class MaterialComponent : Component
    {
        [DataField("materials", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<int, MaterialPrototype>))]
        // ReSharper disable once CollectionNeverUpdated.Local
        public readonly Dictionary<string, int> _materials = new();
        public List<string> MaterialIds => _materials.Keys.ToList();

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

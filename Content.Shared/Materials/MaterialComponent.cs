using Robust.Shared.GameStates;
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
        public readonly Dictionary<string, int> Materials = new();
    }
}

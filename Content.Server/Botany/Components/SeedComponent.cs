using Content.Server.Botany.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components
{
    [RegisterComponent, Friend(typeof(BotanySystem))]
    public sealed class SeedComponent : Component
    {
        [DataField("seed", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<SeedPrototype>))]
        public string SeedName = default!;
    }
}

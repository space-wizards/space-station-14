using Content.Server.Botany.Systems;
using Content.Shared.Botany.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components
{
    [RegisterComponent, Access(typeof(BotanySystem))]
    public sealed partial class SeedComponent : SharedSeedComponent
    {
        /// <summary>
        ///     Seed data containing information about the plant type & properties that this seed can grow seed. If
        ///     null, will instead attempt to get data from a seed prototype, if one is defined. See <see
        ///     cref="SeedId"/>.
        /// </summary>
        [DataField("seed")]
        public SeedData? Seed;

        /// <summary>
        ///     Name of a base seed prototype that is used if <see cref="Seed"/> is null.
        /// </summary>
        [DataField("seedId", customTypeSerializer:typeof(PrototypeIdSerializer<SeedPrototype>))]
        public string? SeedId;
    }
}

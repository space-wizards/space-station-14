using System;
using Content.Server.Advertisements;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Advertise
{
    [RegisterComponent, Friend(typeof(AdvertiseSystem))]
    public class AdvertiseComponent : Component
    {
        public override string Name => "Advertise";

        /// <summary>
        ///     Minimum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal to 1.
        /// </summary>
        [ViewVariables]
        [DataField("minWait")]
        public int MinimumWait { get; } = 8 * 60;

        /// <summary>
        ///     Maximum time in seconds to wait before saying a new ad, in seconds. Has to be larger than or equal
        ///     to <see cref="MinimumWait"/>
        /// </summary>
        [ViewVariables]
        [DataField("maxWait")]
        public int MaximumWait { get; } = 10 * 60;

        /// <summary>
        ///     The identifier for the advertisements pack prototype.
        /// </summary>
        [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<AdvertisementsPackPrototype>))]
        public string PackPrototypeId { get; } = string.Empty;

        /// <summary>
        ///     The next time an advertisement will be said.
        /// </summary>
        [ViewVariables]
        public TimeSpan NextAdvertisementTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        ///     Whether the entity will say advertisements or not.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;
    }
}

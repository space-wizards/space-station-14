using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Numerics;
using Content.Shared.Sanity;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Maps;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Sanity.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SanityComponent : Component
    {

        [ViewVariables]
        public int lvl = 100;

        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan CheckDuration = TimeSpan.FromSeconds(5);


        [DataField("nextCheckTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan NextCheckTime = TimeSpan.Zero;

    }

    [ByRefEvent]
    public readonly record struct SanityEvent();


}

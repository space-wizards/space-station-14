using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Glue
{
    [RegisterComponent, NetworkedComponent]
    public sealed class GlueComponent : Component
    {
    }
}

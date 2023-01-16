using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Throwing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed class ThrownItemComponent : Component
    {
        [AutoNetworkedField]
        public EntityUid? Thrower { get; set; }
    }
}

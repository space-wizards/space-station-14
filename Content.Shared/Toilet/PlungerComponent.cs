
using Robust.Shared.GameStates;

namespace Content.Shared.Toilet
{
    [RegisterComponent, NetworkedComponent,AutoGenerateComponentState]
    public sealed partial class PlungerComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public float PlungeDuration = 2f;
    }
}

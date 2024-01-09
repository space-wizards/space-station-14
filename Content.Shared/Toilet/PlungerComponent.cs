using Robust.Shared.Audio;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Random;

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

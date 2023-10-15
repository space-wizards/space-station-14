using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class StaminaProtectionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("coefficient")]
    public float Coefficient = 1.0f;
}
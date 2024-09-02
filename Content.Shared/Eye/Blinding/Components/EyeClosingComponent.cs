using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Allows mobs to toggle their eyes between being closed and being not closed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class EyeClosingComponent : Component
{
    /// <summary>
    /// The prototype to grant to enable eye-toggling action.
    /// </summary>
    [DataField("eyeToggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EyeToggleAction = "ActionToggleEyes";

    /// <summary>
    /// The actual eye toggling action entity itself.
    /// </summary>
    [DataField]
    public EntityUid? EyeToggleActionEntity;

    /// <summary>
    /// Path to sound to play when opening eyes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public string EyeOpenSound = "/Audio/Effects/eye_open.ogg";

    /// <summary>
    /// Path to sound to play when closing eyes
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public string EyeCloseSound = "/Audio/Effects/eye_close.ogg";

    /// <summary>
    /// Toggles whether the eyes are open or closed. This is really just exactly what it says on the tin. Honest.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool EyesClosed;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool PreviousEyelidPosition;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public bool NaturallyCreated;
}

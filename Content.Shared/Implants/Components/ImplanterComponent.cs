using System.Threading;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants.Components;
[RegisterComponent, NetworkedComponent]
public sealed class ImplanterComponent : Component
{
    /// <summary>
    /// The time it takes to implant someone else
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("implantTime")]
    public float ImplantTime = 5f;

    /// <summary>
    /// Good for single-use injectors
    /// </summary>
    [ViewVariables]
    [DataField("implantOnly")]
    public bool ImplantOnly = false;

    /// <summary>
    /// The current mode of the implanter
    /// </summary>
    [ViewVariables]
    [DataField("currentMode")]
    public ImplanterToggleMode CurrentMode;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool UiUpdateNeeded;

    public CancellationTokenSource? CancelToken;
}

[Serializable, NetSerializable]
public sealed class ImplanterComponentState : ComponentState
{
    public ImplanterToggleMode CurrentMode;

    public ImplanterComponentState(ImplanterToggleMode currentMode)
    {
        CurrentMode = currentMode;
    }
}

[Serializable, NetSerializable]
public enum ImplanterToggleMode : byte
{
    Inject,
    Draw
}

[Serializable, NetSerializable]
public enum ImplanterVisuals : byte
{
    Full
}

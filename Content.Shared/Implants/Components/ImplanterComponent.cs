using System.Threading;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Implants.Components;
/// <summary>
/// Implanters are used to implant or extract implants from an entity
/// Some can be single use (implant only) or some can draw out an implant
/// </summary>
//TODO: Rework drawing to work with implant cases when surgery is in
[RegisterComponent, NetworkedComponent]
public sealed class ImplanterComponent : Component
{
    /// <summary>
    /// The time it takes to implant someone else
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("implantTime")]
    public float ImplantTime = 5f;

    //TODO: Remove when surgery is a thing
    /// <summary>
    /// The time it takes to extract an implant from someone
    /// It's excessively long to deter from implant checking any antag
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("drawTime")]
    public float DrawTime = 300f;

    /// <summary>
    /// Good for single-use injectors
    /// </summary>
    [ViewVariables]
    [DataField("implantOnly")]
    public bool ImplantOnly = false;

    /// <summary>
    /// The current mode of the implanter
    /// Mode is changed automatically depending if it implants or draws
    /// </summary>
    [ViewVariables]
    [DataField("currentMode")]
    public ImplanterToggleMode CurrentMode;

    /// <summary>
    /// The name and description of the implant to show on the implanter
    /// </summary>
    [ViewVariables]
    [DataField("implantData")]
    public (string, string) ImplantData;

    /// <summary>
    /// Grab the <see cref="ItemSlot"/> for this implanter
    /// </summary>
    [ViewVariables]
    public ItemSlot ImplanterSlot = default!;

    public bool UiUpdateNeeded;

    public CancellationTokenSource? CancelToken;
}

[Serializable, NetSerializable]
public sealed class ImplanterComponentState : ComponentState
{
    public ImplanterToggleMode CurrentMode;
    public bool ImplantOnly;

    public ImplanterComponentState(ImplanterToggleMode currentMode, bool implantOnly)
    {
        CurrentMode = currentMode;
        ImplantOnly = implantOnly;
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

[Serializable, NetSerializable]
public enum ImplanterImplantOnlyVisuals : byte
{
    ImplantOnly
}

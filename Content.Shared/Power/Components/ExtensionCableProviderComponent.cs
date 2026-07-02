using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ExtensionCableProviderComponent : Component
{
    /// <summary>
    ///     The max distance this can connect to <see cref="ExtensionCableReceiverComponent"/>s from.
    /// </summary>
    [DataField]
    public int TransferRange { get; set; } = 3;

    [ViewVariables]
    public HashSet<EntityUid> LinkedReceivers { get; } = new();

    /// <summary>
    ///     If <see cref="ExtensionCableReceiverComponent"/>s should consider connecting to this.
    /// </summary>
    [ViewVariables]
    public bool Connectable { get; set; } = true;
}

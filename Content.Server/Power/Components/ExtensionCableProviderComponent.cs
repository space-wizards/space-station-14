using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components;

[RegisterComponent]
[Access(typeof(ExtensionCableSystem))]
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

namespace Content.Shared.Power.Components;

[RegisterComponent]
public sealed partial class ExtensionCableReceiverComponent : Component
{
    [DataField]
    public EntityUid? Provider { get; set; }

    [ViewVariables]
    public bool Connectable = false;

    /// <summary>
    ///     The max distance from a <see cref="ExtensionCableProviderComponent"/> that this can receive power from.
    /// </summary>
    [DataField]
    public int ReceptionRange { get; set; } = 3;
}

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true), AutoGenerateComponentPause]
public sealed partial class VendingMachineEjectComponent : Component
{
    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Deny" state.
    /// Used by the client to determine how long the deny animation should be played.
    /// </summary>
    [DataField]
    public TimeSpan DenyDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Used by the server to determine how long the vending machine stays in the "Eject" state.
    /// The selected item is dispensed after this delay.
    /// Used by the client to determine how long the eject animation should be played.
    /// </summary>
    [DataField]
    public TimeSpan EjectDelay = TimeSpan.FromSeconds(1.2);

    [ViewVariables]
    public bool Ejecting => EjectEnd != null;

    [ViewVariables]
    public bool Denying => DenyEnd != null;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? EjectEnd;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? DenyEnd;

    public EntProtoId? NextItemToEject;

    public bool ThrowNextItem;

    /// <summary>
    /// While disabled by EMP it randomly ejects items.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmpEject = TimeSpan.Zero;

    /// <summary>
    /// Sound that plays when ejecting an item.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
    {
        Params = new AudioParams
        {
            Volume = -4f,
            Variation = 0.15f
        }
    };

    /// <summary>
    /// Sound that plays when an item can't be ejected.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    /// <summary>
    /// How much force to apply when the vending machine throws an item instead of dispensing it normally.
    /// </summary>
    [DataField]
    public float NonLimitedEjectForce = 7.5f;

    /// <summary>
    /// The maximum absolute X and Y values used to pick a random direction for thrown items.
    /// </summary>
    [DataField]
    public float NonLimitedEjectRange = 5f;
}

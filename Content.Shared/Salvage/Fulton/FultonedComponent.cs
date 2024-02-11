using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage.Fulton;

/// <summary>
/// Marks an entity as pending being fultoned.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class FultonedComponent : Component
{
    /// <summary>
    /// Effect entity to delete upon removing the component. Only matters clientside.
    /// </summary>
    [ViewVariables, DataField("effect"), AutoNetworkedField]
    public EntityUid Effect { get; set; }

    [ViewVariables(VVAccess.ReadWrite), DataField("beacon")]
    public EntityUid? Beacon;

    [ViewVariables(VVAccess.ReadWrite), DataField("fultonDuration"), AutoNetworkedField]
    public TimeSpan FultonDuration = TimeSpan.FromSeconds(45);

    /// <summary>
    /// When the fulton is travelling to the beacon.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextFulton", customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextFulton;

    [ViewVariables(VVAccess.ReadWrite), DataField("sound"), AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg");

    // Mainly for admemes.
    /// <summary>
    /// Can the fulton be removed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("removeable")]
    public bool Removeable = true;
}

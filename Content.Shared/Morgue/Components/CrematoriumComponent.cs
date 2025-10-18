using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Morgue.Components;

/// <summary>
/// Allows an entity storage to dispose bodies by turning them into ash.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CrematoriumComponent : Component
{
    /// <summary>
    /// The entity to spawn when something was burned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId LeftOverProtoId = "Ash";

    /// <summary>
    /// The time it takes to cremate something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CookTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The timestamp at which cremating is finished.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan ActiveUntil = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier CremateStartSound = new SoundPathSpecifier("/Audio/Items/Lighters/lighter1.ogg");

    [DataField]
    public SoundSpecifier CrematingSound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");

    [DataField]
    public SoundSpecifier CremateFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}

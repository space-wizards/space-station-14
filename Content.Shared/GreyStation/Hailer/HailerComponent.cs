using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.GreyStation.Hailer;

/// <summary>
/// Gives this clothing a hailer action to shout a random phrase and play a sound.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedHailerSystem))]
public sealed partial class HailerComponent : Component
{
    /// <summary>
    /// Action to grant when worn that uses <see cref="HailerActionEvent"/>.
    /// Tries to open the line picker menu.
    /// Only gets put on cooldown when the line is said.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> PickerAction = string.Empty;

    [DataField]
    public EntityUid? PickerActionEntity;

    /// <summary>
    /// Action to grant when worn that uses <see cref="HailerActionEvent"/>.
    /// Plays a random line every time.
    /// Only gets put on cooldown when the line is said.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> RandomAction = string.Empty;

    [DataField]
    public EntityUid? RandomActionEntity;

    /// <summary>
    /// The inventory slot flags required for the action to be added.
    /// </summary>
    [DataField]
    public SlotFlags RequiredFlags = SlotFlags.MASK;

    /// <summary>
    /// Last played random message, used to make it feel more random.
    /// </sumamry>
    public string LastPlayed = string.Empty;

    /// <summary>
    /// Lines to choose from when out of combat mode and not emagged.
    /// </summary>
    [DataField(required: true)]
    public List<HailerLine> Normal = new();

    /// <summary>
    /// Lines to choose from when in combat mode and not emagged.
    /// </summary>
    [DataField(required: true)]
    public List<HailerLine> Combat = new();

    /// <summary>
    /// Lines to choose from when emagged.
    /// </summary>
    [DataField(required: true)]
    public List<HailerLine> Emagged = new();
}

/// <summary>
/// Action event to open the radial menu for playing a line.
/// If there is only 1 line then it gets played immediately.
/// </summary>
public sealed partial class HailerActionEvent : InstantActionEvent
{
    /// <summary>
    /// Skip the picker and just play a random line.
    /// </summary>
    [DataField]
    public bool Random;
}

/// <summary>
/// A line and sound to be randomly chosen when using the hailer action.
/// </summary>
[DataRecord]
public record struct HailerLine()
{
    /// <summary>
    /// Message to say in chat
    /// </summary>
    [DataField(required: true)]
    public string Message = string.Empty;

    /// <summary>
    /// Tooltip to show in the radial menu instead of the message.
    /// </summary>
    [DataField]
    public string? Tooltip;

    /// <summary>
    /// Sound to be played
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Icon to show in the radial menu.
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("Interface/Actions/scream.png"));
}

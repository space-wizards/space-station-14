using Content.Shared.Decals;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Denotes an object that can be used to alter the appearance of paintable objects (e.g. doors, gas canisters).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SprayPainterComponent : Component
{
    public const string DefaultPickedColor = "red";
    public static readonly ProtoId<DecalPrototype> DefaultDecal = "Arrows";

    /// <summary>
    /// The sound to be played after painting the entities.
    /// </summary>
    [DataField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// The amount of time it takes to paint a pipe.
    /// </summary>
    [DataField]
    public TimeSpan PipeSprayTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The cost of spray painting a pipe, in charges.
    /// </summary>
    [DataField]
    public int PipeChargeCost = 1;

    /// <summary>
    /// Pipe color chosen to spray with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PickedColor = DefaultPickedColor;

    /// <summary>
    /// Pipe colors that can be selected.
    /// </summary>
    [DataField]
    public Dictionary<string, Color> ColorPalette = new();

    /// <summary>
    /// Spray paintable object styles selected per object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> StylesByGroup = new();

    /// <summary>
    /// The currently open tab of the painter
    /// (Are you selecting canister color?)
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SelectedTab;

    /// <summary>
    /// Whether or not the painter should be painting or removing decals when clicked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DecalPaintMode DecalMode = DecalPaintMode.Off;

    /// <summary>
    /// The currently selected decal prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DecalPrototype> SelectedDecal = DefaultDecal;

    /// <summary>
    /// The color in which to paint the decal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? SelectedDecalColor;

    /// <summary>
    /// The angle at which to paint the decal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SelectedDecalAngle;

    /// <summary>
    /// The angle at which to paint the decal.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SnapDecals = true;

    /// <summary>
    /// The cost of spray painting a decal, in charges.
    /// </summary>
    [DataField]
    public int DecalChargeCost = 1;

    /// <summary>
    /// How long does the painter leave items as freshly painted?
    /// </summary>
    [DataField]
    public TimeSpan FreshPaintDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The sound to play when swapping between decal modes.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundSwitchDecalMode = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg", AudioParams.Default.WithVolume(1.5f));
}

/// <summary>
/// A set of operating modes for decal painting.
/// </summary>
[Serializable, NetSerializable]
public enum DecalPaintMode : byte
{
    /// <summary>
    /// Clicking on the floor does nothing.
    /// </summary>
    Off = 0,
    /// <summary>
    /// Clicking on the floor adds a decal at the requested spot (or snapped to the grid)
    /// </summary>
    Add = 1,
    /// <summary>
    /// Clicking on the floor removes all decals at the requested spot (or snapped to the grid)
    /// </summary>
    Remove = 2,
}

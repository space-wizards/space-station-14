using Content.Shared.Decals;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Denotes an object that can be used to alter the appearance of paintable objects (e.g. doors, gas canisters).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprayPainterComponent : Component
{
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
    /// The currently selected color by its key, null if none selected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PickedColor;

    /// <summary>
    /// Pipe colors that can be selected.
    /// </summary>
    [DataField]
    public Dictionary<string, Color> ColorPalette = new();

    /// <summary>
    /// Spray paintable object styles selected per object.
    /// After prototype reload this might not be the same style but it will never be out of bounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, int> Indexes = new();

    /// <summary>
    /// The currently open tab of the painter
    /// (Are you selecting canister color?)
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SelectedTab;

    /// <summary>
    /// Whether or not the painter should be painting decals.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsPaintingDecals = false;

    /// <summary>
    /// The currently selected decal prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DecalPrototype>? SelectedDecal;

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

    [DataField]
    public SoundSpecifier SoundSwitchMode = new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg", AudioParams.Default.WithVolume(1.5f));
}

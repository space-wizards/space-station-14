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
    /// The currently selected colour by its key, null if none selected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PickedColor;

    /// <summary>
    /// A map of selectable colours by key.
    /// </summary>
    [DataField]
    public Dictionary<string, Color> ColorPalette = new();

    /// <summary>
    /// A map of indices by key.
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
    /// Whether or not the open tab has decals.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSelectedTabWithDecals = false;

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
    /// The cost of spray painting a decal, in charges.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DecalChargeCost = 1;

    /// <summary>
    /// How long does the painter leave items as freshly painted?
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FreshPaintDuration = TimeSpan.FromMinutes(15);
}

using Content.Shared.Decals;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprayPainterComponent : Component
{
    [DataField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    [DataField]
    public TimeSpan PipeSprayTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Pipe color chosen to spray with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PickedColor;

    [DataField]
    public Dictionary<string, Color> ColorPalette = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> Indexes = new();

    [DataField, AutoNetworkedField]
    public int SelectedTab;

    [DataField, AutoNetworkedField]
    public ProtoId<DecalPrototype>? SelectedDecal;

    [DataField, AutoNetworkedField]
    public Color? SelectedDecalColor;

    [DataField, AutoNetworkedField]
    public int SelectedDecalAngle;
}

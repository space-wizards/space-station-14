using Content.Shared.Gibbing.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gibbing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GibbingSystem))]
public sealed partial class GibbableComponent : Component
{
    [DataField(required:true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> GibPrototypes = new();

    [DataField(required:true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int GibCount = 3;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib");

    public const float GibScatterRange = 0.3f;

    public static readonly AudioParams GibAudioParams = AudioParams.Default.WithVariation(0.025f);

}

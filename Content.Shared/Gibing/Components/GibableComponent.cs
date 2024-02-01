using Content.Shared.Gibing.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gibing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GibingSystem))]
public sealed partial class GibableComponent : Component
{
    [DataField(required:true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> GibletPrototypes = new();

    [DataField(required:true), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int GibletCount;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib");

}

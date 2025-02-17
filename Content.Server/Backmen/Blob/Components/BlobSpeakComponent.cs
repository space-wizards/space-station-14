using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Backmen.Blob.Components;

[RegisterComponent]
public sealed partial class BlobSpeakComponent : Component
{
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Hivemind";

    [DataField]
    public bool OverrideName = true;

    [DataField]
    public LocId Name = "speak-vv-blob";
}

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Analyzers;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HandheldAnalyzerSystem))]
public sealed partial class HandheldAnalyzerComponent : Component
{
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public SoundSpecifier? StartScanSound;

    [DataField]
    public SoundSpecifier? EndScanSound;

    [DataField]
    public bool Silent;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true)]
    public Enum UiKey;
}

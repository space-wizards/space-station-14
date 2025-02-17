// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Necromorphs.NecroobeliskStoper;

/// <summary>
/// This is used to temporarily disable the obelisk.
/// </summary>
[RegisterComponent, Access(typeof(SharedNecroobeliskSystem))]
public sealed partial class NecroobeliskStoperComponent : Component
{
    [DataField("scanDoAfterDuration")]
    public float ScanDoAfterDuration = 5;

    [DataField("completeSound")]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Effects/radpulse8.ogg");
}

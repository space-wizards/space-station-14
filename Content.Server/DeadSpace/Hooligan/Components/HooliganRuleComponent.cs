// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Hooligan;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Hooligan.Components;

/// <summary>
/// Маркер игрового правила Хулигана.
/// Так же хранит звук для брифинга.
/// </summary>
[RegisterComponent, Access(typeof(HooliganRuleSystem))]
public sealed partial class HooliganRuleComponent : Component
{
    [DataField]
    public SoundSpecifier GreetSound = new SoundPathSpecifier("/Audio/_DeadSpace/Hooligan/hooligan-greetings.ogg");
}

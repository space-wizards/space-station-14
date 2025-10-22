// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Languages.Components;

[RegisterComponent]
public sealed partial class LearnLanguageImplantComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<LanguagePrototype>> Language = new();
}

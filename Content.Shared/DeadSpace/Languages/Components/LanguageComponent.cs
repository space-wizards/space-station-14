// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Languages.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageComponent : Component
{
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> KnownLanguages = new();

    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> CantSpeakLanguages = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<LanguagePrototype> SelectedLanguage = default!;

    [DataField]
    public EntProtoId SelectLanguageAction = "SelectLanguageAction";

    [DataField]
    public EntityUid? SelectLanguageActionEntity;
}

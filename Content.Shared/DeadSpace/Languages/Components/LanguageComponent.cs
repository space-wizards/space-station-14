// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Languages.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<LanguagePrototype>> KnownLanguages = new();

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<LanguagePrototype>> CantSpeakLanguages = new();

    /// <summary>
    ///     Языки, требующие разблокировки для возможности выбора после получения разума в EntityEffectEvent.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<LanguagePrototype>> UnlockLanguagesAfterMakeSentient = new();

    [DataField]
    public string SelectedLanguage = String.Empty;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId SelectLanguageAction = "SelectLanguageAction";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SelectLanguageActionEntity;
}

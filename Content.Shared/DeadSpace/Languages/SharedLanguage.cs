// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;
using Content.Shared.Actions;
using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Languages;

public sealed partial class SelectLanguageActionEvent : InstantActionEvent {}

[Serializable, NetSerializable]
public sealed partial class RequestLanguageMenuEvent : EntityEventArgs
{
    public int Target { get; }
    public readonly HashSet<ProtoId<LanguagePrototype>> KnownLanguages = new();
    public readonly HashSet<ProtoId<LanguagePrototype>> CantSpeakLanguages = new();

    public RequestLanguageMenuEvent(int target, HashSet<ProtoId<LanguagePrototype>> knownLanguages, HashSet<ProtoId<LanguagePrototype>> cantSpeakLanguages)
    {
        Target = target;
        KnownLanguages = knownLanguages;
        CantSpeakLanguages = cantSpeakLanguages;
    }
}

[Serializable, NetSerializable]
public sealed partial class SelectLanguageEvent : EntityEventArgs
{
    public int Target { get; }
    public ProtoId<LanguagePrototype> PrototypeId { get; }

    public SelectLanguageEvent(int target, ProtoId<LanguagePrototype> prototypeId)
    {
        Target = target;
        PrototypeId = prototypeId;
    }
}

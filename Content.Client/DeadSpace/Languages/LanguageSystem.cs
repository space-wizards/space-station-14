// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Client.Audio;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Languages;

public sealed class LanguageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void PlayEntityLexiconSound(AudioParams audioParams, EntityUid sourceUid, ProtoId<LanguagePrototype> languageId)
    {
        if (!_prototypeManager.TryIndex(languageId, out var languageProto))
            return;

        if (languageProto.LexiconSound != null)
            _audio.PlayEntity(_audio.ResolveSound(languageProto.LexiconSound), Filter.Empty(), sourceUid, false, audioParams);
    }

    public void PlayGlobalLexiconSound(AudioParams audioParams, ProtoId<LanguagePrototype> languageId)
    {
        if (!_prototypeManager.TryIndex(languageId, out var languageProto))
            return;

        if (languageProto.LexiconSound != null)
            _audio.PlayGlobal(_audio.ResolveSound(languageProto.LexiconSound), Filter.Empty(), false, audioParams);
    }
}

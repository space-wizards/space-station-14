using System.Collections.Frozen;
using Content.Client.Chat.MarkupTags;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;

namespace Content.Client.Chat.Managers;

public sealed class ContentMarkupTagManager : ISharedContentMarkupTagManager
{
    public IReadOnlyDictionary<string, IContentMarkupTag> ContentMarkupTagTypes => new IContentMarkupTag[]
    {
        new ColorValueContentTag(),
        new EntityNameHeaderContentTag(),
        new SessionNameHeaderContentTag(),
        new SpeechVerbContentTag(),
        new CodewordsContentTag(),
        new PlayAudioContentTag(),
        new AccentContentTag(),
    }.ToFrozenDictionary(x => x.Name, x => x);
}

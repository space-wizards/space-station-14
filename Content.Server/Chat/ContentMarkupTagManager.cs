using System.Collections.Frozen;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;

namespace Content.Server.Chat;

public sealed class ContentMarkupTagManager : ISharedContentMarkupTagManager
{
    // This dictionary should contain serverside-only ContentMarkupTags.
    public IReadOnlyDictionary<string, IContentMarkupTag> ContentMarkupTagTypes => new IContentMarkupTag[]
    {

    }.ToFrozenDictionary(x => x.Name, x => x);
}

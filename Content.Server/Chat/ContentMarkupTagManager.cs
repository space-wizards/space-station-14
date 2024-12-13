using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Chat.Testing;

namespace Content.Server.Chat;

public sealed class ContentMarkupTagManager : ISharedContentMarkupTagManager
{
    public Dictionary<string, IContentMarkupTag> ContentMarkupTagTypes => new IContentMarkupTag[]
    {
        new TestContentTag(),
    }.ToDictionary(x => x.Name, x => x);
}

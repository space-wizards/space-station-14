using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Chat.Testing;

namespace Content.Server.Chat;

public sealed class ContentMarkupTagManager : ISharedContentMarkupTagManager
{

    // This dictionary should contain serverside-only ContentMarkupTags.
    public Dictionary<string, IContentMarkupTag> ContentMarkupTagTypes => new IContentMarkupTag[]
    {

    }.ToDictionary(x => x.Name, x => x);
}

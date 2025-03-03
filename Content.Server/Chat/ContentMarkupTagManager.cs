using System.Collections.Frozen;
using Content.Shared.Chat;
using Content.Shared.Chat.ContentMarkupTags;

namespace Content.Server.Chat;

public sealed class ContentMarkupTagManager : SharedContentMarkupTagManagerBase
{
    // This dictionary should contain serverside-only ContentMarkupTags.
    protected override IReadOnlyDictionary<string, ContentMarkupTagBase> ContentMarkupTagTypes => new ContentMarkupTagBase[]
    {

    }.ToFrozenDictionary(x => x.Name, x => x);
}

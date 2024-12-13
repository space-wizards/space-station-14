using System.Diagnostics;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Decals;
using FastAccessors;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class EntityNameHeaderContentTag : IContentMarkupTag
{
    public string Name => "EntityNameHeader";

    public List<MarkupNode>? OpenerProcessing(MarkupNode node)
    {

        var list = new List<MarkupNode>();
        var name = node.Value.StringValue;

        if (name == null)
            return null;

        list.Add(new MarkupNode(name));

        return list;
    }
}

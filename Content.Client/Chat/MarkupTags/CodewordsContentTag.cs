using System.Diagnostics;
using Content.Shared.Chat.ContentMarkupTags;
using Content.Shared.Mind;
using Content.Shared.Roles.RoleCodeword;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.Chat.MarkupTags;

public sealed class CodewordsContentTag : IContentMarkupTag
{
    // This can, in the future, be refactored into a generalized word highlighting tag.

    public string Name => "Codewords";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _ent = default!;

    public List<MarkupNode>? TextNodeProcessing(MarkupNode node, int randomSeed)
    {
        IoCManager.InjectDependencies(this);

        if (_entSys.TryGetEntitySystem<SharedMindSystem>(out var mindSystem) &&
            _player.LocalUser != null && mindSystem.TryGetMind(_player.LocalUser.Value, out var mindId) &&
            _ent.TryGetComponent(mindId, out RoleCodewordComponent? codewordComp))
        {
            var baseMsg = new FormattedMessage();
            baseMsg.PushTag(node);

            foreach (var (_, codewordData) in codewordComp.RoleCodewords)
            {
                foreach (var codeword in codewordData.Codewords)
                {
                    baseMsg.InsertAroundString(new MarkupNode("color", new MarkupParameter(codewordData.Color), null), codeword, false);
                }
            }
            return new List<MarkupNode>(baseMsg.Nodes);
        }
        return new List<MarkupNode>() {node} ;
    }
}

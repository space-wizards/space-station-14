#nullable enable
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public class PopupEveryone : IGraphAction
    {
        [DataField("text")] public string Text { get; } = string.Empty;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            entity.PopupMessageEveryone(Text);
        }
    }
}

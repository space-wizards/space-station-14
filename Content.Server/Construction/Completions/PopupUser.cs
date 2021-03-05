#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class PopupUser : IGraphAction
    {
        [DataField("cursor")] public bool Cursor { get; private set; } = false;
        [DataField("text")] public string Text { get; private set; } = string.Empty;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (user == null) return;

            if(Cursor)
                user.PopupMessageCursor(Text);
            else
                entity.PopupMessage(user, Text);
        }
    }
}

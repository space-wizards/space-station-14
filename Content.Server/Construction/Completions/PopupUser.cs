#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class PopupUser : IGraphAction
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Text, "text", string.Empty);
            serializer.DataField(this, x => x.Cursor, "cursor", false);
        }

        public bool Cursor { get; private set; } = false;
        public string Text { get; private set; } = string.Empty;

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

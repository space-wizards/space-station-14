using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    public class PopupUser : IEdgeCompleted, IStepCompleted
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Text, "text", string.Empty);
            serializer.DataField(this, x => x.Cursor, "cursor", false);
        }

        public bool Cursor { get; private set; }
        public string Text { get; private set; }

        public async Task StepCompleted(IEntity entity, IEntity user)
        {
            await Completed(entity, user);
        }

        public async Task Completed(IEntity entity, IEntity user)
        {
            if(Cursor)
                user.PopupMessageCursor(Text);
            else
                entity.PopupMessage(user, Text);
        }
    }
}

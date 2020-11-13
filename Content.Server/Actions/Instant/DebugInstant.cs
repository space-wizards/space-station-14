using Content.Server.Utility;
using Content.Shared.Actions;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions.Instant
{
    /// <summary>
    /// Just shows a popup message.
    /// </summary>
    public class DebugInstant : IInstantAction
    {
        public string Message { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Message, "message", "Instant action used.");
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(Message);
        }
    }
}

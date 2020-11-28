using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Actions.Toggle
{
    [UsedImplicitly]
    public class DebugToggle : IToggleAction
    {
        public string MessageOn { get; private set; }
        public string MessageOff { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.MessageOn, "messageOn", "on!");
            serializer.DataField(this, x => x.MessageOff, "messageOff", "off!");
        }

        public void DoToggleAction(ToggleActionEventArgs args)
        {
            if (args.ToggledOn)
            {
                args.Performer.PopupMessageEveryone(MessageOn);
            }
            else
            {
                args.Performer.PopupMessageEveryone(MessageOff);
            }
        }
    }
}

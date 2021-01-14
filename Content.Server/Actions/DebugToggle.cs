using Content.Server.Utility;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class DebugToggle : IToggleAction, IToggleItemAction
    {
        public string MessageOn { get; private set; }
        public string MessageOff { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.MessageOn, "messageOn", "on!");
            serializer.DataField(this, x => x.MessageOff, "messageOff", "off!");
        }

        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (args.ToggledOn)
            {
                args.Performer.PopupMessageEveryone(args.Item.Name + ": " + MessageOn);
            }
            else
            {
                args.Performer.PopupMessageEveryone(args.Item.Name + ": " +MessageOff);
            }

            return true;
        }

        public bool DoToggleAction(ToggleActionEventArgs args)
        {
            if (args.ToggledOn)
            {
                args.Performer.PopupMessageEveryone(MessageOn);
            }
            else
            {
                args.Performer.PopupMessageEveryone(MessageOff);
            }

            return true;
        }
    }
}

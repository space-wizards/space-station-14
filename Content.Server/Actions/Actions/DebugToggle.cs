using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Behaviors.Item;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DebugToggle : IToggleAction, IToggleItemAction
    {
        [DataField("messageOn")] public string MessageOn { get; private set; } = "on!";
        [DataField("messageOff")] public string MessageOff { get; private set; } = "off!";

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

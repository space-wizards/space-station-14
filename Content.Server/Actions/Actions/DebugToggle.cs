using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Behaviors.Item;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class DebugToggle : IToggleAction, IToggleItemAction
    {
        [DataField("messageOn")] public string MessageOn { get; private set; } = "on!";
        [DataField("messageOff")] public string MessageOff { get; private set; } = "off!";

        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (args.ToggledOn)
            {
                args.Performer.PopupMessageEveryone(entMan.GetComponent<MetaDataComponent>(args.Item).EntityName + ": " + MessageOn);
            }
            else
            {
                args.Performer.PopupMessageEveryone(entMan.GetComponent<MetaDataComponent>(args.Item).EntityName + ": " +MessageOff);
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

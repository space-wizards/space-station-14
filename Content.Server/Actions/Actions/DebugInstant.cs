using Content.Server.Popups;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Cooldown;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions.Actions
{
    /// <summary>
    /// Just shows a popup message.asd
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class DebugInstant : IInstantAction, IInstantItemAction
    {
        [DataField("message")] public string Message { get; [UsedImplicitly] private set; } = "Instant action used.";
        [DataField("cooldown")] public float Cooldown { get; [UsedImplicitly] private set; }

        public void DoInstantAction(InstantItemActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(Message);
            if (Cooldown > 0)
            {
                args.ItemActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(Cooldown));
            }
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(Message);
            args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(Cooldown));
        }
    }
}

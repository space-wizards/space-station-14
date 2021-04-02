using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Actions
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

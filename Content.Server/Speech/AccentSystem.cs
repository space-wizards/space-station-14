using System.Text.RegularExpressions;
using Content.Server.Chat.Managers;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Speech
{
    public class AccentSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;

        public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?])", RegexOptions.Compiled);

        public override void Initialize()
        {
            _chatManager.RegisterChatTransform(AccentHandler);
        }

        public string AccentHandler(EntityUid playerUid, string message)
        {
            var accentEvent = new AccentGetEvent(playerUid, message);

            RaiseLocalEvent(playerUid, accentEvent);

            return accentEvent.Message;
        }
    }

    public class AccentGetEvent : EntityEventArgs
    {
        /// <summary>
        ///     The entity to apply the accent to.
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     The message to apply the accent transformation to.
        ///     Modify this to apply the accent.
        /// </summary>
        public string Message { get; set; }

        public AccentGetEvent(EntityUid entity, string message)
        {
            Entity = entity;
            Message = message;
        }
    }
}

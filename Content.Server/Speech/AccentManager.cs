using System.Text.RegularExpressions;
using Content.Server.Chat.Managers;
using Content.Server.Speech.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Speech
{
    public interface IAccentManager
    {
        public void Initialize();
    }

    public class AccentManager : IAccentManager
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?])");

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);

            _chatManager.RegisterChatTransform(AccentHandler);
        }

        public string AccentHandler(IEntity player, string message)
        {
            //TODO: give accents a prio?
            var accents = _entityManager.GetComponents<IAccentComponent>(player.Uid);
            foreach (var accent in accents)
            {
                message = accent.Accentuate(message);
            }
            return message;
        }
    }
}

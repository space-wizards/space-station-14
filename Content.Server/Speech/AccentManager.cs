using System.Text.RegularExpressions;
using Content.Server.Interfaces.Chat;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    public interface IAccentManager
    {
        public void Initialize();
    }

    public class AccentManager : IAccentManager
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;

        public static readonly Regex SentenceRegex = new(@"(?<=[\.!\?])");

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);

            _chatManager.RegisterChatTransform(AccentHandler);
        }

        public string AccentHandler(IEntity player, string message)
        {
            //TODO: give accents a prio?
            var accents = _componentManager.GetComponents<IAccentComponent>(player.Uid);
            foreach (var accent in accents)
            {
                message = accent.Accentuate(message);
            }
            return message;
        }
    }
}

using Content.Server.Interfaces.Chat;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    public interface IAccentManager
    {
        public void Initialize();
    }

    public class AccentManager : IAccentManager
    {
        public static readonly Regex SentenceRegex = new Regex(@"(?<=[\.!\?])");

        public void Initialize()
        {
            IoCManager.Resolve<IChatManager>().RegisterChatTransform(AccentHandler);
        }

        public string AccentHandler(IEntity player, string message)
        {
            //TODO: give accents a prio?
            var accents = IoCManager.Resolve<IComponentManager>().GetComponents<IAccentComponent>(player.Uid);
            foreach (var accent in accents)
            {
                message = accent.Accentuate(message);
            }
            return message;
        }
    }
}

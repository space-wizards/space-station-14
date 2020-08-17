using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Linq;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    internal interface IAccentComponent
    {
        /// <summary>
        /// Transforms a message with the given Accent
        /// </summary>
        /// <param name="message">The spoken message</param>
        /// <returns>The message after the transformation</returns>
        public string Accentuate(string message);
    }

    public class AddAccent : IClientCommand
    {
        public string Command => "addaccent";

        public string Description => "Add a speech component to the current player";

        public string Help => $"{Command} <component>/list";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length == 0)
            {
                shell.SendText(player, Help);
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.SendText(player, "You don't have a player!");
                return;
            }

            // TODO: multiple speech components?
            if (player.AttachedEntity.TryGetComponent(out IAccentComponent speech))
            {
                shell.SendText(player, "You already have a accent!");
                return;
            }
            
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            
            if (args[0] == "list")
            {
                // Get all components that implement the ISpeechComponent except 
                var speeches = compFactory.GetAllRefTypes()
                .Where(c => typeof(IAccentComponent).IsAssignableFrom(c) && c.IsClass);
                var msg = "";
                foreach(var s in speeches)
                {
                    msg += $"{compFactory.GetRegistration(s).Name}\n";
                }
                shell.SendText(player, msg);
            }
            else
            {
                var name = args[0];
                // Try to get the Component
                try
                {
                    var comp = compFactory.GetComponent(name);
                }
                catch (Exception e)
                {
                    shell.SendText(player, $"Accent {name} not found. Try {Command} list to get a list of all appliable accents.");
                    return;
                }
                //TODO: actually give them the component
                player.AttachedEntity.EnsureComponent<BackwardsAccentComponent>();
            }
        }
    }
}

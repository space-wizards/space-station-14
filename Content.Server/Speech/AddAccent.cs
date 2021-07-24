using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Speech.Components;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Speech
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddAccent : IConsoleCommand
    {
        public string Command => "addaccent";
        public string Description => "Add a speech component to the current player";
        public string Help => $"{Command} <component>/?";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine("You don't have an entity!");
                return;
            }

            if (args.Length == 0)
            {
                shell.WriteLine(Help);
                return;
            }

            var compFactory = IoCManager.Resolve<IComponentFactory>();

            if (args[0] == "?")
            {
                // Get all components that implement the ISpeechComponent except
                var speeches = compFactory.GetAllRefTypes()
                    .Where(c => typeof(IAccentComponent).IsAssignableFrom(c) && c.IsClass);
                var msg = new StringBuilder();

                foreach(var s in speeches)
                {
                    msg.Append($"{compFactory.GetRegistration(s).Name}\n");
                }

                shell.WriteLine(msg.ToString());
            }
            else
            {
                var name = args[0];

                // Try to get the Component
                if (!compFactory.TryGetRegistration(name, out var registration, true))
                {
                    shell.WriteLine($"Accent {name} not found. Try {Command} ? to get a list of all applicable accents.");
                    return;
                }

                var type = registration.Type;

                // Check if that already exists
                if (player.AttachedEntity.HasComponent(type))
                {
                    shell.WriteLine("You already have this accent!");
                    return;
                }

                // Generic fuckery
                var ensure = typeof(IEntity).GetMethod("AddComponent");
                if (ensure == null)
                    return;
                var method = ensure.MakeGenericMethod(type);
                method.Invoke(player.AttachedEntity, null);
            }
        }
    }
}

#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class ListGasesCommand : IClientCommand
    {
        public string Command => "listgases";
        public string Description => "Prints a list of gases and their indices.";
        public string Help => "listgases";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            var atmosSystem = EntitySystem.Get<AtmosphereSystem>();

            foreach (var gasPrototype in atmosSystem.Gases)
            {
                shell.SendText(player, $"{gasPrototype.Name} ID: {gasPrototype.ID}");
            }
        }
    }

}

using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetSolutionCapacity : IConsoleCommand
    {
        public string Command => "setsolutioncapacity";
        public string Description => "Set the capacity (maximum volume) of some solution.";
        public string Help => $"Usage: {Command} <target> <solution> <new capacity>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
            {
                shell.WriteLine($"Not enough arguments.\n{Help}");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteLine($"Invalid entity id.");
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(uid, out SolutionContainerManagerComponent? man))
            {
                shell.WriteLine($"Entity does not have any solutions.");
                return;
            }

            if (!man.Solutions.ContainsKey(args[1]))
            {
                var validSolutions = string.Join(", ", man.Solutions.Keys);
                shell.WriteLine($"Entity does not have a \"{args[1]}\" solution. Valid solutions are:\n{validSolutions}");
                return;
            }
            var solution = man.Solutions[args[1]];

            if (!float.TryParse(args[2], out var quantityFloat))
            {
                shell.WriteLine($"Failed to parse new capacity.");
                return;
            }

            if(quantityFloat < 0.0f)
            {
                shell.WriteLine($"Cannot set the maximum volume of a solution to a negative number.");
                return;
            }

            var quantity = FixedPoint2.New(quantityFloat);
            EntitySystem.Get<SolutionContainerSystem>().SetCapacity(uid, solution, quantity);
        }
    }
}

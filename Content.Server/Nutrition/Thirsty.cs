using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Console;

namespace Content.Server.Nutrition
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class Thirsty : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "thirsty";
        public string Description => "Makes you thirsty.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command unless you are a player.");
                return;
            }

            if (player.AttachedEntity is not {Valid: true} playerEntity)
            {
                shell.WriteLine("You cannot use this command without an entity.");
                return;
            }

            if (!_entities.TryGetComponent(playerEntity, out ThirstComponent? thirst))
            {
                shell.WriteLine($"Your entity does not have a {nameof(ThirstComponent)} component.");
                return;
            }

            var thirstyThreshold = thirst.ThirstThresholds[ThirstThreshold.Parched];
            _entities.System<ThirstSystem>().SetThirst(playerEntity, thirst, thirstyThreshold);
        }
    }
}

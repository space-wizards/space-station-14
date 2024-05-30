using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class Hungry : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPrototypeManager _prototypes = default!;

        private readonly ProtoId<SatiationTypePrototype> _satiationHunger = "hunger";

        public string Command => "hungry";
        public string Description => "Makes you hungry.";
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

            if (!_entities.TryGetComponent(playerEntity, out SatiationComponent? component))
            {
                shell.WriteLine($"Your entity does not have a {nameof(SatiationComponent)} component.");
                return;
            }

            if (!component.Satiations.AsReadOnly().TryGetValue(_satiationHunger, out var satiation))
            {
                shell.WriteLine($"Your entity does not have a {_satiationHunger} satiation meter.");
                return;
            }

            if (!_prototypes.TryIndex(satiation.Prototype, out var prototype))
            {
                shell.WriteLine($"Your entity has invalid satiation prototype id.");
                return;
            }

            var hungryThreshold = prototype.Thresholds[SatiationThreashold.Desperate];
            _entities.System<SatiationSystem>().SetSatiation((playerEntity, component), _satiationHunger, hungryThreshold);
        }
    }
}

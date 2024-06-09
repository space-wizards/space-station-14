using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition;

[AdminCommand(AdminFlags.Debug)]
public sealed class Unsatiate : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public string Command => "unsatiate";
    public string Description => "Makes you desperate of provided satiations types.";
    public string Help => $"{Command} <SatiationType> [<SatiationType>...]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine("No satiation type provided.");
            return;
        }

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

        foreach (var satiationType in args)
        {
            if (!_entities.System<SatiationSystem>().TryGetSatiationPrototype((playerEntity, component), satiationType, out var satiationPrototype))
            {
                shell.WriteLine($"Your entity does not have a {satiationType} satiation meter.");
                return;
            }

            if (!_prototypes.TryIndex(satiationPrototype, out var prototype))
            {
                shell.WriteLine($"Your entity has invalid satiation prototype id.");
                return;
            }

            var satiationThreshold = prototype.Thresholds[SatiationThreashold.Desperate];
            _entities.System<SatiationSystem>().SetSatiation((playerEntity, component), satiationType, satiationThreshold);
        }
    }
}

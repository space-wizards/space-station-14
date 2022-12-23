using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class BriefCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "brief";
        public string Description => "Makes you a mob of choice until the command is rerun.";
        public string Help => $"Usage: {Command} <outfit> <name> <entity>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Nah");
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("You can't spawn here!");
                return;
            }

            if (mind.VisitingEntity != default)
            {
                var entity = mind.VisitingEntity;
                player.ContentData()!.Mind?.UnVisit();

                foreach (var officer in _entities.EntityQuery<BriefOfficerComponent>(true))
                {
                    if (officer.Owner == entity) _entities.QueueDeleteEntity(officer.Owner);
                }
                return;
            }

            var coordinates = player.AttachedEntity != null
                ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
                : EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();

            var entname = args[2];
            if (entname == "") entname = "MobHuman";

            var brief = _entities.SpawnEntity(entname, coordinates);
            _entities.EnsureComponent<BriefOfficerComponent>(brief);
            _entities.GetComponent<TransformComponent>(brief).AttachToGridOrMap();

            if (args[1] != "") _entities.GetComponent<MetaDataComponent>(brief).EntityName = args[1];

            mind.Visit(brief);

            string outfit = "";
            if (_prototypeManager.Index<StartingGearPrototype>(args[0]) != null) outfit = args[0];
            if (outfit == "")
            {
                Console.WriteLine("An error occurred while trying to set the brief outfit.");
                return;
            }

            SetOutfitCommand.SetOutfit(brief, outfit, _entities);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<StartingGearPrototype>()
                    .Select(p => new CompletionOption(p.ID))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString("brief-command-arg-outfit"));
            }
            if (args.Length == 2)
            {
                // what a great solution
                List<string> list = new();
                list.Add("");
                return CompletionResult.FromHintOptions(list, Loc.GetString("brief-command-arg-name"));
            }
            if (args.Length == 3)
            {
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => p.ID.StartsWith("Mob"))
                    .Select(p => new CompletionOption(p.ID))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString("brief-command-arg-species"));
            }

            return CompletionResult.Empty;
        }
    }
}

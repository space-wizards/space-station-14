using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands.Brief
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class BriefCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IEntitySystemManager _entitysys = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "brief";
        public string Description => "Makes you a mob of choice until the command is rerun.";
        public string Help => $"Usage: {Command} <outfit> <name> <entity> <force>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You aren't a player.");
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("You can't spawn without a mind.");
                return;
            }

            if (mind.VisitingEntity != default)
            {
                var didYouBrief = false;
                if (args.Length >= 4)
                    if (args[3].ToLower() == "true")
                        didYouBrief = true;

                foreach (var officer in _entities.EntityQuery<BriefOfficerComponent>(true))
                {
                    if (officer.Owner == mind.VisitingEntity)
                    {
                        didYouBrief = true;
                        player.ContentData()!.Mind?.UnVisit();
                        _entities.QueueDeleteEntity(officer.Owner);
                        return;
                    }
                }

                if (didYouBrief == false)
                {
                    shell.WriteError("You are visiting something other than a brief already.");
                    return;
                }
            }

            var outfit = "CentcomGear";
            if (args.Length >= 1)
                if (_prototypeManager.TryIndex<StartingGearPrototype>(args[0], out var outfitProto))
                    outfit = outfitProto.ID;
                else
                {
                    shell.WriteError("Given outfit is invalid.");
                    return;
                }

            var coordinates = player.AttachedEntity != null
                ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
                : _entitysys.GetEntitySystem<GameTicker>().GetObserverSpawnPoint();

            var entName = "MobHuman";
            if (args.Length >= 3)
                entName = args[2];
            _prototypeManager.TryIndex<EntityPrototype>(entName, out var entProto);
            if (entProto == null)
            {
                shell.WriteError("Entity prototype is invalid.");
                return;
            }

            var brief = _entities.SpawnEntity(entName, coordinates);
            _entities.EnsureComponent<BriefOfficerComponent>(brief);
            _entities.TryGetComponent<TransformComponent>(brief, out var briefTransform);
            if (briefTransform != null)
                briefTransform.AttachToGridOrMap();

            if (args.Length >= 2)
                _entities.GetComponent<MetaDataComponent>(brief).EntityName = args[1];

            mind.Visit(brief);
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
            if (args.Length == 4)
            {
                // what a great solution 2
                List<string> list = new();
                list.Add("true");
                list.Add("false");
                return CompletionResult.FromHintOptions(list, Loc.GetString("brief-command-arg-force"));
            }

            return CompletionResult.Empty;
        }
    }
}

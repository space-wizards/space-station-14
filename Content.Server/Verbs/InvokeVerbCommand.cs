using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Verbs
{
    [AdminCommand(AdminFlags.Admin)]
    public class InvokeVerbCommand : IConsoleCommand
    {
        public string Command => "invokeverb";
        public string Description => Loc.GetString("invoke-verb-command-description");
        public string Help => Loc.GetString("invoke-verb-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var verbSystem = EntitySystem.Get<SharedVerbSystem>();

            // get the 'player' entity (defaulting to command user, otherwise uses a uid)
            IEntity? playerEntity = null;
            if (!int.TryParse(args[0], out var intPlayerUid))
            {
                if (args[0] == "self" && shell.Player?.AttachedEntity != null)
                {
                    playerEntity = shell.Player.AttachedEntity;
                }
                else
                {
                    return;
                }
            }
            else
            {
                entityManager.TryGetEntity(new EntityUid(intPlayerUid), out playerEntity);
            }

            // gets the target entity
            if (int.TryParse(args[1], out var intUid))
            {
                var entUid = new EntityUid(intUid);

                if (playerEntity != null && entityManager.TryGetEntity(entUid, out var target))
                {
                    var verbName = args[2].ToLowerInvariant();
                    var verbs = verbSystem.GetLocalVerbs(
                        target, playerEntity, VerbType.All, true
                        );

                    foreach (var (type, set) in verbs)
                    {
                        if (type == VerbType.Interaction && verbName == "interaction"
                         || type == VerbType.Activation && verbName == "activation"
                         || type == VerbType.Alternative && verbName == "alternative")
                        {
                            verbSystem.ExecuteVerb(set.First());
                            return;
                        }
                        foreach (var verb in set)
                        {
                            if (verb.Text.ToLowerInvariant() == verbName)
                            {
                                verbSystem.ExecuteVerb(verb);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}

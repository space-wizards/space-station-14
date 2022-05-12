using System.Linq;
using Content.Server.Act;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Components;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared.MobState.Components;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class SuicideCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "suicide";

        public string Description => Loc.GetString("suicide-command-description");

        public string Help => Loc.GetString("suicide-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SuicideSystem>().Suicide(shell);
        }
    }
}

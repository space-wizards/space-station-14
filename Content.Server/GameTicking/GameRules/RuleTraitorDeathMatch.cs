using Content.Server.Interfaces.Chat;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Players;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameTicking.GameRules
{
    public class RuleTraitorDeathMatch : GameRule
    {
        // This class only exists so that the game rule is available for the conditional spawner.
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Commands
{
    /// <summary>
    /// Utilities for writing commands
    /// </summary>
    public static class CommandUtils
    {
        /// <summary>
        /// Gets the player session for the player with the indicated id,
        /// sending a failure to the performer if unable to.
        /// </summary>
        public static bool TryGetSessionByUsernameOrId(IConsoleShell shell,
            string usernameOrId, ICommonSession performer, [NotNullWhen(true)] out ICommonSession? session)
        {
            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (plyMgr.TryGetSessionByUsername(usernameOrId, out session)) return true;
            if (Guid.TryParse(usernameOrId, out var targetGuid))
            {
                if (plyMgr.TryGetSessionById(new NetUserId(targetGuid), out session)) return true;
                shell.WriteLine("Unable to find user with that name/id.");
                return false;
            }

            shell.WriteLine("Unable to find user with that name/id.");
            return false;
        }

        /// <summary>
        /// Gets the attached entity for the player session with the indicated id,
        /// sending a failure to the performer if unable to.
        /// </summary>
        public static bool TryGetAttachedEntityByUsernameOrId(IConsoleShell shell,
            string usernameOrId, ICommonSession performer, out EntityUid attachedEntity)
        {
            attachedEntity = default;
            if (!TryGetSessionByUsernameOrId(shell, usernameOrId, performer, out var session)) return false;
            if (session.AttachedEntity == null)
            {
                shell.WriteLine("User has no attached entity.");
                return false;
            }

            attachedEntity = session.AttachedEntity.Value;
            return true;
        }

        public static string SubstituteEntityDetails(IConsoleShell shell, EntityUid ent, string ruleString)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var transform = entMan.GetComponent<TransformComponent>(ent);

            // gross, is there a better way to do this?
            ruleString = ruleString.Replace("$ID", ent.ToString());
            ruleString = ruleString.Replace("$WX",
                transform.WorldPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$WY",
                transform.WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LX",
                transform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$LY",
                transform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
            ruleString = ruleString.Replace("$NAME", entMan.GetComponent<MetaDataComponent>(ent).EntityName);

            if (shell.Player is { } player)
            {
                if (player.AttachedEntity is {Valid: true} p)
                {
                    var pTransform = entMan.GetComponent<TransformComponent>(p);

                    ruleString = ruleString.Replace("$PID", ent.ToString());
                    ruleString = ruleString.Replace("$PWX",
                        pTransform.WorldPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PWY",
                        pTransform.WorldPosition.Y.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLX",
                        pTransform.LocalPosition.X.ToString(CultureInfo.InvariantCulture));
                    ruleString = ruleString.Replace("$PLY",
                        pTransform.LocalPosition.Y.ToString(CultureInfo.InvariantCulture));
                }
            }
            return ruleString;
        }
    }
}

using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Paper;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.CommandReport
{
    public class CommandReportSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        private const string AnnouncementSound = "/Audio/Announcements/commandreport.ogg";
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        ///     Broadcast a command report to the radio or through command console
        /// </summary>
        /// <returns>True if at least one communication console got the report</returns>
        public bool SendCommandReport(bool broadcastToRadio, string message)
        {
            // Such copypaste code... So sorry...
            var wasSent = false;
            var messageWrap = Loc.GetString("command-reports-sender-announcement-wrap-message");

            SoundSystem.Play(AnnouncementSound, Filter.Broadcast(), AudioParams.Default.WithVolume(-2f));  // Play the "New Command Report Created" sound effect

            _chatManager.ChatMessageToAll(ChatChannel.Radio, broadcastToRadio == true ? message : Loc.GetString("command-reports-confidential-announcement-message"), messageWrap, colorOverride: Color.Gold);

            // some copypaste code from NukeCodeSystem.cs, to the person making fax, don't forget to convert this too!
            var consoles = EntityManager.EntityQuery<CommunicationsConsoleComponent>(); // get all entities with the CommunicationConsoleComponent
            foreach (var console in consoles)
            {
                if (!EntityManager.TryGetComponent((console).Owner, out TransformComponent? transform))
                    continue; // if no transform component, then ignore and continue

                var consolePos = transform.MapPosition;
                var paper = EntityManager.SpawnEntity("Paper", consolePos); // the paper that will be sent to the console

                var papercomp = EntityManager.GetComponent<PaperComponent>(paper); // the PaperComponent of the paper
                var papermeta = EntityManager.GetComponent<MetaDataComponent>(paper); // the metadata of the paper

                papercomp.Content = $"Central Command Report\n{message}"; // header + contents
                papermeta.EntityName = Loc.GetString("command-reports-paper-name"); // set the name of the paper to something special

                wasSent = true; // fax has been sent at least once
            }

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Command report sent: {message}");

            return wasSent;
        }
    }
}

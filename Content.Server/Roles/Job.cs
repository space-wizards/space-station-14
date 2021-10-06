using Content.Server.Chat.Managers;
using Content.Shared.Roles;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Roles
{
    public class Job : Role
    {
        public JobPrototype Prototype { get; }

        public override string Name { get; }
        public override bool Antagonist => false;

        public string? StartingGear => Prototype.StartingGear;

        public Job(Mind.Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            Prototype = jobPrototype;
            Name = jobPrototype.Name;
        }

        public override void Greet()
        {
            base.Greet();

            if (Mind.TryGetSession(out var session))
            {
                var chat = IoCManager.Resolve<IChatManager>();
                chat.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name", ("jobName", Name)));

                if(Prototype.RequireAdminNotify)
                    chat.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));

                chat.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", Name), ("supervisors", Prototype.Supervisors)));

                if(Prototype.JoinNotifyCrew && Mind.CharacterName != null)
                    chat.DispatchStationAnnouncement(Loc.GetString("job-greet-join-notify-crew", ("jobName", Name), ("characterName", Mind.CharacterName)),
                        Loc.GetString("job-greet-join-notify-crew-announcer"));
            }
        }
    }
}

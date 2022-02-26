using Content.Server.Chat.Managers;
using Content.Shared.Roles;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Roles
{
    public sealed class Job : Role
    {
        [ViewVariables]
        public JobPrototype Prototype { get; }

        public override string Name { get; }

        public override bool Antagonist => false;

        [ViewVariables]
        public string? StartingGear => Prototype.StartingGear;

        [ViewVariables]
        public bool CanBeAntag;

        public Job(Mind.Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            Prototype = jobPrototype;
            Name = jobPrototype.Name;
            CanBeAntag = jobPrototype.CanBeAntag;
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
                        Loc.GetString("job-greet-join-notify-crew-announcer"), false);
            }
        }
    }
}

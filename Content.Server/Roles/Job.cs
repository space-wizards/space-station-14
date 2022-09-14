using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Roles
{
    public sealed class Job : Role, IRoleTimer
    {
        [ViewVariables] public string Timer => Prototype.PlayTimeTracker;

        [ViewVariables]
        public JobPrototype Prototype { get; }

        public override string Name { get; }

        public override bool Antagonist => false;

        [ViewVariables]
        public string? StartingGear => Prototype.StartingGear;

        [ViewVariables]
        public string? JobEntity => Prototype.JobEntity;

        [ViewVariables]
        public bool CanBeAntag;

        public Job(Mind.Mind mind, JobPrototype jobPrototype) : base(mind)
        {
            Prototype = jobPrototype;
            Name = jobPrototype.LocalizedName;
            CanBeAntag = jobPrototype.CanBeAntag;
        }

        public override void Greet()
        {
            base.Greet();

            if (Mind.TryGetSession(out var session))
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                var chatSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
                chatMgr.DispatchServerMessage(session, Loc.GetString("job-greet-introduce-job-name", ("jobName", Name)));

                if(Prototype.RequireAdminNotify)
                    chatMgr.DispatchServerMessage(session, Loc.GetString("job-greet-important-disconnect-admin-notify"));

                chatMgr.DispatchServerMessage(session, Loc.GetString("job-greet-supervisors-warning", ("jobName", Name), ("supervisors", Loc.GetString(Prototype.Supervisors))));

                if(Prototype.JoinNotifyCrew && Mind.CharacterName != null)
                {
                    if (Mind.OwnedEntity != null)
                    {
                        chatSys.DispatchStationAnnouncement(Mind.OwnedEntity.Value,
                            Loc.GetString("job-greet-join-notify-crew", ("jobName", Name),
                                ("characterName", Mind.CharacterName)),
                            Loc.GetString("job-greet-join-notify-crew-announcer"), false);
                    }
                }
            }
        }
    }
}

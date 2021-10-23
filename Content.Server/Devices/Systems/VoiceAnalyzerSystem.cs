using Content.Shared.Devices;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Devices.Systems
{
    public class VoiceAnalyzerSystem : EntitySystem
    {
        [Dependency]
        private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<SharedVoiceAnalyzerComponent, GetOtherVerbsEvent>(AddConfigureVerb);
        }

        private void AddConfigureVerb(EntityUid uid, SharedVoiceAnalyzerComponent component, GetOtherVerbsEvent args)
        {

            if (!args.CanAccess)
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                if (!EntityManager.TryGetComponent<ActorComponent>(args.User.Uid, out var actorComponent))
                    return;
                _userInterfaceSystem.TryOpen(uid, VoiceAnalyzerUiKey.Key, actorComponent.PlayerSession);
            };
            verb.Text = "Configure";
            args.Verbs.Add(verb);

        }
    }
}

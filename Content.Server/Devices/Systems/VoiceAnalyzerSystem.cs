using System;
using Content.Server.Chat.Managers;
using Content.Shared.Devices;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;


namespace Content.Server.Devices.Systems
{
    public class VoiceAnalyzerSystem : EntitySystem
    {
        [Dependency]
        private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<SharedVoiceAnalyzerComponent, GetOtherVerbsEvent>(AddConfigureVerb);

            //Bound UI messages
            SubscribeLocalEvent<SharedVoiceAnalyzerComponent, VoiceAnalyzerUpdateModeMessage>(OnModeUpdateRequested);
            SubscribeLocalEvent<SharedVoiceAnalyzerComponent, VoiceAnalyzerUpdateTextMessage>(OnTextUpdateRequested);
        }

        private void OnTextUpdateRequested(EntityUid uid, SharedVoiceAnalyzerComponent component, VoiceAnalyzerUpdateTextMessage args)
        {
            component.VoiceQueryText = args.VoiceText;
        }

        private void OnModeUpdateRequested(EntityUid uid, SharedVoiceAnalyzerComponent component, VoiceAnalyzerUpdateModeMessage args)
        {
            var vaEnum = (SharedVoiceAnalyzerComponent.AnalyzeMode) Enum.ToObject
                (typeof(SharedVoiceAnalyzerComponent.AnalyzeMode) , args.ModeEnum);

            var owner = EntityManager.GetEntity(args.Entity);
            //If we've somehow been passed an invalid enum, that probably means the client is cheating.
            if (!Enum.IsDefined(typeof(SharedVoiceAnalyzerComponent.AnalyzeMode), vaEnum))
            {
                Logger.Warning($"Received invalid enum for voice analyzer from {owner}. Suspected cheater.");
                return;
            }

            component.Mode = vaEnum;
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

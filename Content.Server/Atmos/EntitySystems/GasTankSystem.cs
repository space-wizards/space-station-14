using Content.Server.Atmos.Components;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public class GasTankSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private const float TimerDelay = 0.5f;
        private float _timer = 0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasTankComponent, GetActivationVerbsEvent>(AddOpenUIVerb);
        }

        private void AddOpenUIVerb(EntityUid uid, GasTankComponent component, GetActivationVerbsEvent args)
        {
            if (!args.CanAccess ||  !args.User.TryGetComponent<ActorComponent>(out var actor))
                return;

            Verb verb = new();
            verb.Act = () => component.OpenInterface(actor.PlayerSession);
            verb.Text = Loc.GetString("control-verb-open-control-panel-text");
            // TODO VERBS add "open UI" icon?
            args.Verbs.Add(verb);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay) return;
            _timer -= TimerDelay;

            foreach (var gasTank in EntityManager.EntityQuery<GasTankComponent>())
            {
                _atmosphereSystem.React(gasTank.Air, gasTank);
                gasTank.CheckStatus();
                gasTank.UpdateUserInterface();
            }
        }
    }
}

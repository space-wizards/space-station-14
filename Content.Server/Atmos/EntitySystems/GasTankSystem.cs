using Content.Server.Atmos.Components;
using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTankSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private const float TimerDelay = 0.5f;
        private float _timer = 0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GasTankComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUIVerb);
            SubscribeLocalEvent<GasTankComponent, GetActionsEvent>(OnGetActions);
            SubscribeLocalEvent<GasTankComponent, ToggleActionEvent>(OnActionToggle);
        }

        private void OnGetActions(EntityUid uid, GasTankComponent component, GetActionsEvent args)
        {
            args.Actions.Add(component.ToggleAction);
        }

        private void OnActionToggle(EntityUid uid, GasTankComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            component.ToggleInternals();

            args.Handled = true;
        }

        private void AddOpenUIVerb(EntityUid uid, GasTankComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess ||  !EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
                return;

            ActivationVerb verb = new();
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

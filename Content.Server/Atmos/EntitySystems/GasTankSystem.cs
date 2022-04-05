using Content.Server.Atmos.Components;
using Content.Server.UserInterface;
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;
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
            SubscribeLocalEvent<GasTankComponent, BeforeActivatableUIOpenEvent>(BeforeUiOpen);
            SubscribeLocalEvent<GasTankComponent, GetActionsEvent>(OnGetActions);
            SubscribeLocalEvent<GasTankComponent, ToggleActionEvent>(OnActionToggle);
            SubscribeLocalEvent<GasTankComponent, DroppedEvent>(OnDropped);
        }

        private void BeforeUiOpen(EntityUid uid, GasTankComponent component, BeforeActivatableUIOpenEvent args)
        {
            // Only initial update includes output pressure information, to avoid overwriting client-input as the updates come in.
            component.UpdateUserInterface(true);
        }

        private void OnDropped(EntityUid uid, GasTankComponent component, DroppedEvent args)
        {
            component.DisconnectFromInternals(args.User);
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timer += frameTime;

            if (_timer < TimerDelay) return;
            _timer -= TimerDelay;

            foreach (var gasTank in EntityManager.EntityQuery<GasTankComponent>())
            {
                _atmosphereSystem.React(gasTank.Air, gasTank);
                gasTank.CheckStatus(_atmosphereSystem);

                if (gasTank.UserInterface != null && gasTank.UserInterface.SubscribedSessions.Count > 0)
                {
                    gasTank.UpdateUserInterface();
                }
            }
        }
    }
}

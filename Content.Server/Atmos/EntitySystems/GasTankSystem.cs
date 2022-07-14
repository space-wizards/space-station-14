using Content.Server.Atmos.Components;
using Content.Server.UserInterface;
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Examine;
using JetBrains.Annotations;

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
            SubscribeLocalEvent<GasTankComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<GasTankComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<GasTankComponent, ToggleActionEvent>(OnActionToggle);
            SubscribeLocalEvent<GasTankComponent, EntParentChangedMessage>(OnParentChange);
        }

        private void BeforeUiOpen(EntityUid uid, GasTankComponent component, BeforeActivatableUIOpenEvent args)
        {
            // Only initial update includes output pressure information, to avoid overwriting client-input as the updates come in.
            component.UpdateUserInterface(true);
        }

        private void OnParentChange(EntityUid uid, GasTankComponent component, ref EntParentChangedMessage args)
        {
            // When an item is moved from hands -> pockets, the container removal briefly dumps the item on the floor.
            // So this is a shitty fix, where the parent check is just delayed. But this really needs to get fixed
            // properly at some point. 
            component.CheckUser = true;
        }

        private void OnGetActions(EntityUid uid, GasTankComponent component, GetItemActionsEvent args)
        {
            args.Actions.Add(component.ToggleAction);
        }

        private void OnExamined(EntityUid uid, GasTankComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
                args.PushMarkup(Loc.GetString("comp-gas-tank-examine", ("pressure", Math.Round(component.Air?.Pressure ?? 0))));
            if (component.IsConnected)
                args.PushMarkup(Loc.GetString("comp-gas-tank-connected"));
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
                if (gasTank.CheckUser)
                {
                    gasTank.CheckUser = false;
                    if (Transform(gasTank.Owner).ParentUid != gasTank.User)
                    {
                        gasTank.DisconnectFromInternals(gasTank.User);
                        continue;
                    }
                }

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

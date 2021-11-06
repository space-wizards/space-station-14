using System.Linq;
using Content.Shared;
using Content.Shared.CCVar;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.IoC;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ActivatableUIRequiresPowerSystem : EntitySystem
    {
        [Dependency] private readonly ActivatableUISystem _activatableUISystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, ActivatableUIOpenAttemptEvent>(OnActivate);
            SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnActivate(EntityUid uid, ActivatableUIRequiresPowerComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (args.Cancelled) return;
            if (EntityManager.TryGetComponent<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            {
                args.User.PopupMessageCursor(Loc.GetString("base-computer-ui-component-not-powered"));
                args.Cancel();
            }
        }

        private void OnPowerChanged(EntityUid uid, ActivatableUIRequiresPowerComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
                _activatableUISystem.CloseAll(uid);
        }
    }
}

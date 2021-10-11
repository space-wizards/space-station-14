using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Content.Server.Storage.Components;
using Content.Server.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using static Content.Shared.Storage.SharedSuitStorageComponent;
using Robust.Shared.Log;

namespace Content.Server.Storage
{
    internal sealed class SuitStorageSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<SuitStorageComponent, InteractHandEvent>(OnSuitStorageInteractHand);
            SubscribeLocalEvent<SuitStorageComponent, InteractUsingEvent>(OnSuitStorageInteractObject);
        }

        public void UpdateUserInterface(EntityUid uid,
        SuitStorageComponent? suitStorage = null)
        {
            if (!Resolve(uid, ref suitStorage))
                return;

            _userInterfaceSystem.TrySetUiState(uid, SuitStorageUIKey.Key,
                new SuitStorageBoundUserInterfaceState(
                    suitStorage.Open,
                    true
                    ));
        }

        private void OnSuitStorageInteractHand(EntityUid uid, SuitStorageComponent component, InteractHandEvent args)
        {
            UpdateUserInterface(uid, component);
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.UserInterface?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnSuitStorageInteractObject(EntityUid uid, SuitStorageComponent component, InteractUsingEvent args)
        {
            component.AddToContents(args.Used);
        }
    }
}

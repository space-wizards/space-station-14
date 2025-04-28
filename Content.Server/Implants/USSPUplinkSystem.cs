using Content.Server.Store.Systems;
using Content.Shared.Implants;
using Content.Shared.Store;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Actions;
using Content.Shared.Store.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Implants
{
    public sealed class USSPUplinkSystem : EntitySystem
    {
        [Dependency] private readonly StoreSystem _storeSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OpenUplinkImplantEvent>(OnOpenUplinkImplant);
        }

        private void OnOpenUplinkImplant(OpenUplinkImplantEvent args)
        {
            var user = args.User;
            if (!_entityManager.TryGetComponent(user, out StoreComponent? store))
                return;

            // Check if the user has the USSP uplink implant store
            if (!store.Balance.ContainsKey("Telebond"))
                return;

            // Open the USSP uplink UI (StoreBoundUserInterface)
            _storeSystem.ToggleUi(user, store.Owner, store);
            Logger.DebugS("ussp-uplink", $"Opened USSP uplink UI for {ToPrettyString(user)}");
        }
    }
}

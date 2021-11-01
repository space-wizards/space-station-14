using Content.Client.HUD;
using Content.Client.Items.Components;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.Inventory
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : EntitySystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(_ => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();

            SubscribeLocalEvent<ClientInventoryComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<ClientInventoryComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());

            SubscribeLocalEvent<ClientInventoryComponent, SlipAttemptEvent>(OnSlipAttemptEvent);
        }

        // jesus christ, this is duplicated to server/client, should really just be shared..
        private void OnSlipAttemptEvent(EntityUid uid, ClientInventoryComponent component, SlipAttemptEvent args)
        {
            if (component.TryGetSlot(EquipmentSlotDefines.Slots.SHOES, out IEntity? shoes))
            {
                RaiseLocalEvent(shoes.Uid, args, false);
            }
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        private void HandleOpenInventoryMenu()
        {
            _gameHud.InventoryButtonDown = !_gameHud.InventoryButtonDown;
        }
    }
}

using System;
using System.Collections.Generic;
using Content.Server.Labels.Components;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Shared.Cargo;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components
{

    //This entire class is a PLACEHOLDER for the cargo shuttle.
    //welp only need auto-docking now.

    [RegisterComponent]
    public class CargoTelepadComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private const float TeleportDuration = 0.5f;
        private const float TeleportDelay = 15f;
        private List<CargoOrderData> _teleportQueue = new();
        private CargoTelepadState _currentState = CargoTelepadState.Unpowered;
        [DataField("teleportSound")] private SoundSpecifier _teleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

        /// <summary>
        ///     The paper-type prototype to spawn with the order information.
        /// </summary>
        [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrinterOutput = "Paper";

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerUpdate(powerChanged);
                    break;
            }
        }

        public void QueueTeleport(CargoOrderData order)
        {
            for (var i = 0; i < order.Amount; i++)
            {
                _teleportQueue.Add(order);
            }
            TeleportLoop();
        }

        private void PowerUpdate(PowerChangedMessage args)
        {
            if (args.Powered && _currentState == CargoTelepadState.Unpowered) {
                _currentState = CargoTelepadState.Idle;
                if(_entMan.TryGetComponent<SpriteComponent?>(Owner, out var spriteComponent) && spriteComponent.LayerCount > 0)
                    spriteComponent.LayerSetState(0, "idle");
                TeleportLoop();
            }
            else if (!args.Powered)
            {
                _currentState = CargoTelepadState.Unpowered;
                if (_entMan.TryGetComponent<SpriteComponent?>(Owner, out var spriteComponent) && spriteComponent.LayerCount > 0)
                    spriteComponent.LayerSetState(0, "offline");
            }
        }
        private void TeleportLoop()
        {
            if (_currentState == CargoTelepadState.Idle && _teleportQueue.Count > 0)
            {
                _currentState = CargoTelepadState.Charging;
                if (_entMan.TryGetComponent<SpriteComponent?>(Owner, out var spriteComponent) && spriteComponent.LayerCount > 0)
                    spriteComponent.LayerSetState(0, "idle");
                Owner.SpawnTimer((int) (TeleportDelay * 1000), () =>
                {
                    if (!Deleted && !_entMan.Deleted(Owner) && _currentState == CargoTelepadState.Charging && _teleportQueue.Count > 0)
                    {
                        _currentState = CargoTelepadState.Teleporting;
                        if (_entMan.TryGetComponent<SpriteComponent?>(Owner, out var spriteComponent) && spriteComponent.LayerCount > 0)
                            spriteComponent.LayerSetState(0, "beam");
                        Owner.SpawnTimer((int) (TeleportDuration * 1000), () =>
                        {
                            if (!Deleted && !((!_entMan.EntityExists(Owner) ? EntityLifeStage.Deleted : _entMan.GetComponent<MetaDataComponent>(Owner).EntityLifeStage) >= EntityLifeStage.Deleted) && _currentState == CargoTelepadState.Teleporting && _teleportQueue.Count > 0)
                            {
                                SoundSystem.Play(Filter.Pvs(Owner), _teleportSound.GetSound(), Owner, AudioParams.Default.WithVolume(-8f));
                                SpawnProduct(_teleportQueue[0]);
                                _teleportQueue.RemoveAt(0);
                                if (_entMan.TryGetComponent<SpriteComponent?>(Owner, out var spriteComponent) && spriteComponent.LayerCount > 0)
                                    spriteComponent.LayerSetState(0, "idle");
                                _currentState = CargoTelepadState.Idle;
                                TeleportLoop();
                            }
                        });
                    }
                });
            }
        }

        /// <summary>
        ///     Spawn the product and a piece of paper. Attempt to attach the paper to the product.
        /// </summary>
        private void SpawnProduct(CargoOrderData data)
        {
            // spawn the order
            if (!_prototypeManager.TryIndex(data.ProductId, out CargoProductPrototype? prototype))
                return;

            var product = _entMan.SpawnEntity(prototype.Product, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

            _entMan.GetComponent<TransformComponent>(product).Anchored = false;

            // spawn a piece of paper.
            var printed = _entMan.SpawnEntity(PrinterOutput, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
            if (!_entMan.TryGetComponent(printed, out PaperComponent paper))
                return;

            // fill in the order data
            string val = Loc.GetString("cargo-console-paper-print-name", ("orderNumber", data.OrderNumber));
            _entMan.GetComponent<MetaDataComponent>(printed).EntityName = val;
            paper.SetContent(Loc.GetString(
                "cargo-console-paper-print-text",
                ("orderNumber", data.OrderNumber),
                ("requester", data.Requester),
                ("reason", data.Reason),
                ("approver", data.Approver)));

            // attempt to attach the label
            if (_entMan.TryGetComponent(product, out PaperLabelComponent label))
            {
                EntitySystem.Get<ItemSlotsSystem>().TryInsert(Owner, label.LabelSlot, printed, null);
            }
        }

        private enum CargoTelepadState { Unpowered, Idle, Charging, Teleporting };
    }
}

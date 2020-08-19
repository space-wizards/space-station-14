using Content.Server.GameObjects.Components.Interactable;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class ReceiverComponent : Component, IInteractUsing
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override string Name => "Receiver";

        private List<TransmitterComponent> _transmitters;

        public override void Initialize()
        {
            base.Initialize();

            _transmitters = new List<TransmitterComponent>();
        }

        public void DistributeTrigger(bool state)
        {
            foreach (var comp in Owner.GetAllComponents<IReceiver>())
            {
                comp.Trigger(state);
            }
        }

        public void Subscribe(TransmitterComponent transmitter)
        {
            if (_transmitters.Contains(transmitter))
            {
                return;
            }

            _transmitters.Add(transmitter);
        }

        public void Unsubscribe(TransmitterComponent transmitter)
        {
            _transmitters.Remove(transmitter);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool))
                return false;

            if (tool.HasQuality(ToolQuality.Multitool)
                && eventArgs.Using.TryGetComponent<LinkerComponent>(out var linker)
                && linker.Link != null)
            {
                if (!Owner.Transform.GridPosition.InRange(_mapManager, linker.Link.Owner.Transform.GridPosition, linker.Link.Range))
                {
                    _notifyManager.PopupMessage(Owner, eventArgs.User, "Out of range.");
                    return false;
                }

                Subscribe(linker.Link);
                linker.Link.Subscribe(this);
                _notifyManager.PopupMessage(Owner, eventArgs.User, "Linked!");
            }

            return false;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            foreach (var transmitter in _transmitters)
            {
                if (transmitter.Deleted)
                {
                    continue;
                }

                transmitter.Unsubscribe(this);
            }
            _transmitters.Clear();
        }
    }
}

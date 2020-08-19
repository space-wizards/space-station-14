using Content.Server.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class SwitchComponent : Component, IInteractHand, IActivate
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        public override string Name => "Switch";

        private bool _on;

        public override void Initialize()
        {
            base.Initialize();

            _on = false;
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            Trigger(eventArgs.User);
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            Trigger(eventArgs.User);
            return true;
        }

        private void Trigger(IEntity user)
        {
            _on = !_on;

            if (!Owner.TryGetComponent<TransmitterComponent>(out var transmitter))
            {
                return;
            }

            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                if (_on)
                {
                    sprite.LayerSetState(0, "on");
                }
                else
                {
                    sprite.LayerSetState(0, "off");
                }
            }
            _notifyManager.PopupMessage(Owner, user, $"{(_on ? "On" : "Off")}.");
            transmitter.TransmitTrigger(_on);
        }
    }
}

using System;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Mobs.Actions;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Mobs.Actions
{
    [RegisterComponent]
    public class LaserAbilityComponent : SharedLaserAbilityComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IHotbarManager _hotbarManager;
#pragma warning restore 649

        private HotbarAction _hotbarAction;

        public override void Initialize()
        {
            base.Initialize();

            _hotbarAction = new HotbarAction("Laser", "/Textures/Objects/Guns/Laser/laser_retro.rsi/laser_retro.png", TriggerHotbarAction, ToggleHotbarAction);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _hotbarManager.RemoveAction(_hotbarAction);
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case GetActionsMessage msg:
                {
                    msg.Hotbar.AddActionToMenu(_hotbarAction);
                    break;
                }
                // Stuff to make sure it doesn't break when component gets detached and/or reattached.
                case PlayerAttachedMsg msg:
                {
                    _hotbarAction.ActivateAction = TriggerHotbarAction;
                    _hotbarAction.SelectAction = ToggleHotbarAction;
                    break;
                }
                case PlayerDetachedMsg msg:
                {
                    _hotbarAction.ActivateAction = null;
                    _hotbarAction.SelectAction = null;
                    break;
                }
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case FireLaserCooldownMessage msg:
                {
                    _hotbarAction.Start = msg.Start;
                    _hotbarAction.End = msg.End;
                    break;
                }
            }
        }

        private void TriggerHotbarAction(HotbarAction action, ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (!Owner.IsValid())
            {
                return;
            }

            if (_gameTiming.CurTime < _hotbarAction.End) // + TimeSpan(latency) for prediction maybe?
            {
                return;
            }

            SendNetworkMessage(new FireLaserMessage(coords));

            _hotbarManager.UnbindUse(_hotbarAction);
            return;
        }

        private void ToggleHotbarAction(HotbarAction action, bool pressed)
        {
            if (!Owner.IsValid())
            {
                return;
            }

            if (pressed)
            {
                _hotbarManager.BindUse(_hotbarAction);
            }
            else
            {
                _hotbarManager.UnbindUse(_hotbarAction);
            }
            return;
        }
    }
}

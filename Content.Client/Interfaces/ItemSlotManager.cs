using System;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Interfaces
{
    public class ItemSlotManager : IItemSlotManager
    {
#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 0649

        public void Initialize()
        {

        }

        public bool OnButtonPressed(BaseButton.ButtonEventArgs args, IEntity item)
        {
            args.Event.Handle();

            if (item == null)
                return false;

            if (args.Event.Function == ContentKeyFunctions.ExamineEntity)
            {
                _entitySystemManager.GetEntitySystem<ExamineSystem>()
                    .DoExamine(item);
            }
            else if (args.Event.Function == ContentKeyFunctions.OpenContextMenu)
            {
                _entitySystemManager.GetEntitySystem<VerbSystem>()
                                    .OpenContextMenu(item, new ScreenCoordinates(args.Event.PointerLocation.Position));
            }
            else if (args.Event.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                var inputSys = _entitySystemManager.GetEntitySystem<InputSystem>();

                var func = args.Event.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

                var mousePosWorld = _eyeManager.ScreenToWorld(args.Event.PointerLocation);
                var message = new FullInputCmdMessage(_gameTiming.CurTick, funcId, args.Event.State, mousePosWorld,
                    args.Event.PointerLocation, item.Uid);

                // client side command handlers will always be sent the local player session.
                var session = _playerManager.LocalPlayer.Session;
                inputSys.HandleInputCommand(session, func, message);
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}

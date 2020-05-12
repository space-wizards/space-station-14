using System;
using System.Linq;
using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;
using static Robust.Shared.Input.PointerInputCmdHandler;

namespace Content.Client.GameObjects.EntitySystems
{
    public class HotbarSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.OpenAbilitiesMenu,
                InputCmdHandler.FromDelegate(s => HandleOpenAbilitiesMenu()));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar0,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(0, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar1,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(1, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar2,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(2, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar3,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(3, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar4,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(4, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar5,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(5, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar6,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(6, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar7,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(7, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar8,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(8, args); }));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar9,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(9, args); }));
        }

        private void HandleOpenAbilitiesMenu()
        {
            var playerEnt = _playerManager.LocalPlayer.ControlledEntity;
            if (playerEnt == null
                || !playerEnt.TryGetComponent(out HotbarComponent clientHotbar))
            {
                return;
            }

            playerEnt.SendMessage(null, new GetAbilitiesMessage(clientHotbar));

            clientHotbar.AbilityMenu.Open();
        }

        private bool HandleHotbarKeybindPressed(int index, in PointerInputCmdArgs args)
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return false;
            }

            var ability = hotbarComponent.Abilities.ElementAtOrDefault(index);
            if (ability == null)
            {
                return false;
            }

            ability.Activate(args);
            return true;
        }
    }

    public class Ability
    {
        public Texture Texture;
        public Action<ICommonSession, GridCoordinates, EntityUid, Ability> Action;
        public TimeSpan? Start;
        public TimeSpan? End;
        public TimeSpan? Cooldown;

        public Ability(string texturePath, Action<ICommonSession, GridCoordinates, EntityUid, Ability> action, TimeSpan? cooldown)
        {
            var resCache = IoCManager.Resolve<IResourceCache>();
            Texture = resCache.GetTexture(texturePath);
            Action = action;
            Start = null;
            End = null;
            Cooldown = cooldown;
        }

        public void Activate(PointerInputCmdArgs args)
        {
            Action?.Invoke(args.Session, args.Coordinates, args.EntityUid, this);
        }
    }

    public class GetAbilitiesMessage : ComponentMessage
    {
        public HotbarComponent Hotbar;

        public GetAbilitiesMessage(HotbarComponent hotbar)
        {
            Hotbar = hotbar;
        }
    }
}

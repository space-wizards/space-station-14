using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    [RegisterComponent]
    public class HotbarComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        public override string Name => "Hotbar";

        public HotbarGui _gui;
        public AbilityMenu AbilityMenu;
        public List<Ability> Abilities;
        public List<Ability> Hotbar;

        public override void Initialize()
        {
            base.Initialize();

            AbilityMenu = new AbilityMenu();
            _gui = new HotbarGui();
            Abilities = new List<Ability>();
            Hotbar = new List<Ability>();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg msg:
                {
                    SendMessage(new GetAbilitiesMessage(this));
                    if (_gui == null)
                    {
                        _gui = new HotbarGui();
                    }
                    else
                    {
                        _gui.Parent?.RemoveChild(_gui);
                    }

                    _gameHud.HotbarContainer.AddChild(_gui);
                    break;
                }
                case PlayerDetachedMsg _:
                {
                    _gui.Parent?.RemoveChild(_gui);
                    break;
                }
            }
        }

        public void AddAbility(Ability ability)
        {
            Abilities.Add(ability);
            if (Hotbar.Count < 10)
            {
                Hotbar.Add(ability);
            }
        }

        public void SetSlot(int slot, Ability ability)
        {
        }
    }
}

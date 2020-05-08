using System;
using System.Collections.Generic;
using System.Text;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using static Robust.Shared.Input.PointerInputCmdHandler;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    public class HotbarComponent : Component
    {
        public override string Name => "Hotbar";

        
    }

    public class Ability
    {
        private Texture texture;
        private Action action;

        public void Activate(PointerInputCmdArgs args)
        {
            //action?.Invoke(args);
        }
    }
}

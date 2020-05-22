using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.UserInterface
{
    public interface IHotbarManager
    {
        void BindUse(GameObjects.EntitySystems.Ability ability);
        void UnbindUse(GameObjects.EntitySystems.Ability ability);
    }
}

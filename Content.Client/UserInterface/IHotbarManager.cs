using System.Collections.Generic;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.HUD.Hotbar;

namespace Content.Client.UserInterface
{
    public interface IHotbarManager
    {
        void Initialize();
        IReadOnlyDictionary<HotbarActionId, HotbarAction> GetGlobalActions();
        void BindUse(HotbarAction action);
        void UnbindUse(HotbarAction action);
        void RemoveAction(HotbarAction action);
    }
}

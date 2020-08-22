using Content.Shared.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ConstructionGhostComponent : Component, IExamine
    {
        public override string Name => "ConstructionGhost";

        [ViewVariables] public ConstructionPrototype Prototype { get; set; }
        [ViewVariables] public int GhostID { get; set; }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddText(Loc.GetString("Building: {0}\n", Prototype.Name));
            EntitySystem.Get<SharedConstructionSystem>().DoExamine(message, Prototype, 0, inDetailsRange);
        }
    }
}

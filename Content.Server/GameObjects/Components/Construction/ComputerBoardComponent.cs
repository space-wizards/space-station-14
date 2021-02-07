using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class ComputerBoardComponent : Component
    {
        public override string Name => "ComputerBoard";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
        }

        [ViewVariables]
        public string Prototype { get; private set; }
    }
}

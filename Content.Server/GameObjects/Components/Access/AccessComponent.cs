using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    public class AccessComponent : Component
    {
        public override string Name => "Access";
        [ViewVariables]
        public List<string> Tags;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Tags, "tags", new List<string>());
        }
    }
}

using System.Collections.Generic;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class AccessComponent : Component, IAccess
    {
        public override string Name => "Access";
        [ViewVariables]
        private List<string> _tags;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _tags, "tags", new List<string>());
        }

        public List<string> GetTags()
        {
            return _tags;
        }

        public void SetTags(List<string> newTags)
        {
            _tags = newTags;
        }
    }
}

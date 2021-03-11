#nullable enable
using System.Collections.Generic;
using Content.Server.Interfaces;
using Content.Shared.Access;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class AccessComponent : Component, IAccess
    {
        public override string Name => "Access";

        [DataField("tags")]
        [ViewVariables]
        private AccessTags _tags;

        public AccessTags Tags => _tags;
        public bool IsReadOnly => false;

        public void SetTags(AccessTags newTags)
        {
            _tags = newTags;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Shared.Access;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    /// <summary>
    ///     Stores access levels necessary to "use" an entity
    ///     and allows checking if something or somebody is authorized with these access levels.
    /// </summary>
    [RegisterComponent]
    public class AccessReader : Component
    {
        public override string Name => "AccessReader";

        /// <summary>
        ///     The set of tags that will automatically deny an allowed check, if any of them are present.
        /// </summary>
        public HashSet<string> DenyTags = new();

        /// <summary>
        ///     List of access lists to check allowed against. For an access check to pass
        ///     there has to be an access list that is a subset of the access in the checking list.
        /// </summary>
        [DataField("access")]
        [ViewVariables]
        public List<HashSet<string>> AccessLists = new();
    }
}

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.RadioKey.Components
{
    /// <summary>
    /// A HOLDER for radio key prototypes. Can only hold one (1) prototype ID
    /// This is specificaly made for things like radio key (ITEM)
    /// </summary>
    [RegisterComponent]
    public sealed class RadioKeyHolderComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("radioKeyPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<RadioKeyPrototype>))]
        public string RadioKeyPrototype = default!;
    }
}

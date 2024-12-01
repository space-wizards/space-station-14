using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.Devour;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Changeling.Devour;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingIdentityComponent : Component
{
    //TODO: Figure out if it's better to have a list of 1 identity at the start (the original Identity) or have a separate one as a fallback
    [DataField]
    public StoredIdentityComponent? OriginalIdentityComponent;
    [DataField]
    public List<StoredIdentityComponent>? Identities = [];
}

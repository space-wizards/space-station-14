using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.OnCollide;
[RegisterComponent, NetworkedComponent]
public sealed partial class SpriteWhitelistedComponent : Component
{
    [DataField]
    public EntityWhitelist? LocalEntityWhitelist;

    [DataField]
    public Dictionary<string, PrototypeLayerData> PassedLayers = [];

    [DataField]
    public Dictionary<string, PrototypeLayerData> FailedLayers = [];

}

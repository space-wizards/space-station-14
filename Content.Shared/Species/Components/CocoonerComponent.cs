// SPDX-FileCopyrightText: 2025 Drywink <43855731+Drywink@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Species.Arachnid;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCocoonSystem))]
public sealed partial class CocoonerComponent : Component
{
    [DataField("wrapAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), AutoNetworkedField]
    public string WrapAction = "ActionArachnidWrap";

    [DataField("actionEntity"), AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField("wrapDuration"), AutoNetworkedField]
    public float WrapDuration = 10f;

    [DataField("wrapDuration_Short"), AutoNetworkedField]
    public float WrapDuration_Short = 3f;

    [DataField("hungerCost"), AutoNetworkedField]
    public float HungerCost = 10f;

    [DataField("wrapRange"), AutoNetworkedField]
    public float WrapRange = 0.5f;
}

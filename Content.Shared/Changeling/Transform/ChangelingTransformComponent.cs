using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Changeling.Devour;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Changeling.Transform;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedChangelingTransformSystem))]
public sealed partial class ChangelingTransformComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ChangelingTransformAction = "ActionChangelingTransform";

    [DataField]
    public EntityUid? ChangelingTransformActionEntity;

    [DataField]
    public ChangelingIdentityComponent? ChangelingIdentities;

    [DataField]
    public float TransformWindup = 5f;
}


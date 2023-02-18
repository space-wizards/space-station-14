using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class OrganComponent : Component
{
    [DataField("body")] public EntityUid? Body;

    [DataField("parent")] public OrganSlot? ParentSlot;

    [DataField("painModifier")] public FixedPoint2 PainModifier = 1.0f;

    [DataField("rawPain")] public FixedPoint2 RawPain = 0f;

    public FixedPoint2 Pain => PainModifier * RawPain;
}

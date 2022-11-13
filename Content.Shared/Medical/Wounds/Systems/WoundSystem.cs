using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private RobustRandom _random = default!;
    //DamageTypeId
    //private Dictionary<string, List<string>>

    public override void Initialize()
    {
    }





    //TODO: return an out woundhandle from this!
    public bool TryApplyWounds(EntityUid target, DamageSpecifier damage)
    {
        var success = false;
        if (!EntityManager.TryGetComponent<WoundableComponent>(target, out var woundContainer))
            return false;

        EntityManager.TryGetComponent<BodyPartComponent>(target, out var bodyPart);
        //if (bodyPart!.) //TODO: Check if skeleton is external and apply damage to it first if that is the case
        if (EntityManager.TryGetComponent<BodyCoveringComponent>(target, out var covering))
        {
            var coveringResistance = _prototypeManager.Index<BodyCoveringPrototype>(covering.PrimaryBodyCoveringId).Resistance;
            DamageSpecifier.ApplyModifierSet(damage, coveringResistance); //apply covering resistances first!
            //TODO: eventually take into account second skin covering for damage resistance
            DamageSpecifier.ApplyModifierSet(damage, covering.DamageResistance);
            success = TryApplyWoundsCovering(target, covering, damage, woundContainer);
        }
        if (bodyPart != null)
        {
            DamageSpecifier.ApplyModifierSet(damage, bodyPart.DamageResistance);
            success |= TryApplyWoundsBodyPart(target, bodyPart, damage, woundContainer);
        }
        if (EntityManager.TryGetComponent<OrganComponent>(target, out var organ))
        {
            DamageSpecifier.ApplyModifierSet(damage, organ.DamageResistance);
            success |= TryApplyWoundsOrgan(target, organ, damage, woundContainer);
        }
        return success;
    }

    private bool TryApplyWoundsCovering(EntityUid target, BodyCoveringComponent covering, DamageSpecifier damage,
        WoundableComponent woundContainer)
    {
        //TODO: get damage from cached protos
        return true;
    }

    private bool TryApplyWoundsBodyPart(EntityUid target, BodyPartComponent bodyPart, DamageSpecifier damage, WoundableComponent woundContainer)
    {

        return true;
    }
    private bool TryApplyWoundsOrgan(EntityUid target, OrganComponent organ, DamageSpecifier damage, WoundableComponent woundContainer)
    {

        return true;
    }


}

[Serializable, NetSerializable]
[Flags]
public enum WoundDepth
{
    None = 0,
    Surface = 1 <<1,
    Internal = 1 << 2,
    Solid = 1 << 3,
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundData (string WoundId, float Severity, float Tended, float Size, float Infected);

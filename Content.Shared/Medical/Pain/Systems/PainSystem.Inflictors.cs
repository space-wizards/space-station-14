using Content.Shared.Body.Part;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.Systems;

public sealed partial class PainSystem
{
    public bool InflictPain(EntityUid targetEntity, EntityUid inflicterEntity, PainReceiverComponent? receiver = null,
        PainInflicterComponent? inflicter = null, PainLocalModifierComponent? localModifier = null)
    {
        if (!Resolve(targetEntity, ref receiver) || !Resolve(inflicterEntity, ref inflicter))
            return false;
        var painToInflict = inflicter.Pain;
        if (localModifier != null)
        {
            painToInflict *= localModifier.Modifier;
        }
        if (painToInflict == 0)
            return false;
        ApplyPain(targetEntity, receiver, painToInflict);
        return true;
    }
}

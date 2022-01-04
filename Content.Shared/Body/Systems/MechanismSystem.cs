using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Systems;

public class MechanismSystem : EntitySystem
{
    public void SetPart(EntityUid uid, SharedBodyPartComponent? part,
        MechanismComponent? mech = null)
    {
        if (!Resolve(uid, ref mech))
            return;

        if (part == mech.Part)
            return;

        // We're replacing an existing part
        if (mech.Part != null)
        {
            // It's on a body
            if (mech.Part.Body != null)
            {
                RaiseLocalEvent(uid, new RemovedFromPartInBodyEvent(mech.Part.Body, mech.Part), false);
            }
            else
            {
                RaiseLocalEvent(uid, new RemovedFromPartEvent(mech.Part), false);
            }
        }

        if (part != null)
        {
            if (part.Body != null)
            {
                RaiseLocalEvent(uid, new AddedToPartInBodyEvent(part.Body, part), false);
            }
            else
            {
                RaiseLocalEvent(uid, new AddedToPartEvent(part), false);
            }
        }
    }
}

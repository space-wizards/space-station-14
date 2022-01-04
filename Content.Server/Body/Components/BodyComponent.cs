using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Random.Helpers;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedBodyComponent))]
public class BodyComponent : SharedBodyComponent
{
    [DataField("gibSound")] public SoundSpecifier GibSound = new SoundCollectionSpecifier("gib");

    protected override bool CanAddPart(string slotId, SharedBodyPartComponent part)
    {
        return base.CanAddPart(slotId, part) &&
               _partContainer.CanInsert(part.Owner);
    }

    protected override void OnAddPart(BodyPartSlot slot, SharedBodyPartComponent part)
    {
        base.OnAddPart(slot, part);

        _partContainer.Insert(part.Owner);
    }

    protected override void OnRemovePart(BodyPartSlot slot, SharedBodyPartComponent part)
    {
        base.OnRemovePart(slot, part);

        _partContainer.ForceRemove(part.Owner);
        part.Owner.RandomOffset(0.25f);
    }
}

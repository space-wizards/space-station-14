using Content.Server.VoiceMask;
using Content.Shared.Implants;

namespace Content.Server._DV.Implants;

public sealed class SubdermalBionicSyrinxImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceMaskComponent, ImplantImplantedEvent>(OnInsert);
    }

    private void OnInsert(Entity<VoiceMaskComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted is not { } implanted)
            return;

        // Update the name so it's the entities default name. You can't take it off like a voice mask so it's important!
        ent.Comp.VoiceMaskName = Name(implanted);
    }
}

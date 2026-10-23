using Content.Shared._MACRO.Body.Components;
using Content.Shared.Body;
using Content.Shared.Examine;

namespace Content.Shared._MACRO.Body.EntitySystems;

public sealed class OrganAttachedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganAttachedComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<OrganAttachedComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<OrganComponent>(ent.Comp.AttachedOrgan, out var organ) || organ.Body == null)
            return;

        var organBody = organ.Body.Value;

        args.PushMarkup(Loc.GetString(
            "organ-attached-examine",
            ("attached", ent),
            ("organ",ent.Comp.AttachedOrgan),
            ("body", organBody)));
    }
}

using Content.Shared.Atmos;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

/// <summary>
/// Handles reducing fire damage when wearing clothing with <see cref="FireProtectionComponent"/>.
/// </summary>
public sealed class FireProtectionSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireProtectionComponent, InventoryRelayedEvent<GetFireProtectionEvent>>(OnGetProtection);
        SubscribeLocalEvent<FireProtectionComponent, GetVerbsEvent<ExamineVerb>>(OnFireProtectionVerbExamine);
    }

    private void OnGetProtection(Entity<FireProtectionComponent> ent, ref InventoryRelayedEvent<GetFireProtectionEvent> args)
    {
        args.Args.Reduce(ent.Comp.Reduction);
    }

    private void OnFireProtectionVerbExamine(EntityUid uid, FireProtectionComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetFireProtectionExamine(component.Reduction);

        var ev = new FireProtectionExamineEvent(examineMarkup);
        RaiseLocalEvent(uid, ref ev);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            Loc.GetString("fire-protection-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("fire-protection-examinable-verb-message"));
    }

    private FormattedMessage GetFireProtectionExamine(float reduction)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("fire-protection-reduction-value", ("value", MathF.Round((1f - reduction) * 100, 1))));

        return msg;
    }
}

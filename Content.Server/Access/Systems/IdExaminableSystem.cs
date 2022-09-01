using Content.Server.Access.Components;
using Content.Shared.Access.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Access.Systems;

public sealed class IdExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, IdExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {

        var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);
        var info = GetInfo(component.Owner) ?? Loc.GetString("id-examinable-component-verb-no-id");

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = FormattedMessage.FromMarkup(info);
                _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("id-examinable-component-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = Loc.GetString("id-examinable-component-verb-disabled"),
            IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
    }

    private string? GetInfo(EntityUid uid)
    {
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
        {
            // PDA
            if (EntityManager.TryGetComponent(idUid, out PDAComponent? pda) && pda.ContainedID is not null)
            {
                return GetNameAndJob(pda.ContainedID);
            }
            // ID Card
            if (EntityManager.TryGetComponent(idUid, out IdCardComponent? id))
            {
                return GetNameAndJob(id);
            }
        }
        return null;
    }

    private string GetNameAndJob(IdCardComponent id)
    {
        var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

        var val = string.IsNullOrWhiteSpace(id.FullName)
            ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                ("jobSuffix", jobSuffix))
            : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                ("fullName", id.FullName),
                ("jobSuffix", jobSuffix));

        return val;
    }
}

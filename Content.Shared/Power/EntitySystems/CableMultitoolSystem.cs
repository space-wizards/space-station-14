using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Power.EntitySystems;

public abstract partial class CableMultitoolSystem : EntitySystem
{
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteractUsing(Entity<CableComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || args.Target is null || !args.CanReach ||
            !_tool.HasQuality(args.Used, SharedToolSystem.PulseQuality))
            return;

        ExamineCable(ent, args.User);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetExamineVerbs(Entity<CableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        // Must be in details range to try this.
        // Theoretically there should be a separate range at which a multitool works, but this does just fine.
        if (!_examine.IsInDetailsRange(args.User, args.Target))
            return;

        var user = args.User;
        var held = args.Using;

        // Pulsing is hardcoded here because I don't think it needs to be more complex than that right now.
        // Update if I'm wrong.
        var enabled = held is not null && _tool.HasQuality(held.Value, SharedToolSystem.PulseQuality);
        var verb = new ExamineVerb
        {
            Disabled = !enabled,
            Message = Loc.GetString("cable-multitool-system-verb-tooltip"),
            Text = Loc.GetString("cable-multitool-system-verb-name"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Act = () => ExamineCable(ent, user),
        };

        args.Verbs.Add(verb);
    }

    protected virtual void ExamineCable(Entity<CableComponent> ent, EntityUid user) { }
}

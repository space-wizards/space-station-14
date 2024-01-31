using Content.Server.Nutrition.Components;
using Content.Shared.Examine;

namespace Content.Server.Nutrition.EntitySystems;

public sealed partial class SealableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SealableComponent, ExaminedEvent>(OnExamined, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<SealableComponent, OpenableOpenedEvent>(OnOpened);
    }

    private void OnExamined(EntityUid uid, SealableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var sealedText = comp.Sealed ? Loc.GetString(comp.ExamineTextSealed) : Loc.GetString(comp.ExamineTextUnsealed);

        args.PushMarkup(sealedText);
    }

    private void OnOpened(EntityUid entity, SealableComponent comp, OpenableOpenedEvent args)
    {
        comp.Sealed = false;
    }

    /// <summary>
    /// Returns true if the entity's seal is intact.
    /// Items without SealableComponent are considered unsealed.
    /// </summary>
    public bool IsSealed(EntityUid uid, SealableComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        return comp.Sealed;
    }
}

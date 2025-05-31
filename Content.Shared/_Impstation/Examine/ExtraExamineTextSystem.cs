using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content.Shared._Impstation.Examine;

/// <summary>
/// Adds examine text to the entity, intentionally "obvious details".
/// Like, that's it. It's basic -- all it does is add the line to the attached entity.
/// This is particularly used for assigning players unique examine text.
/// </summary>
public sealed class ExtraExamineTextSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExtraExamineTextComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ExtraExamineTextComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.Lines.Count == 0)
        {
            RemCompDeferred<ExtraExamineTextComponent>(entity); // no more need for me!
            return;
        }

        foreach (var l in entity.Comp.Lines)
        {
            args.PushMarkup(l.Value);
        }
    }
}

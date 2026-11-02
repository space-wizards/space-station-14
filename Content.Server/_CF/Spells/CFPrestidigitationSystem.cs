using Content.Server.Construction;
using Content.Shared.CF.Spells;

namespace Content.Server.CF.Spells;

/// <summary>
/// A raw steak is replaced by the cooked entity when the temperature step completes.
/// The flavor has to move onto that new entity or it is deleted with the raw one.
/// </summary>
public sealed class CFPrestidigitationCookSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CFSpellSeasoningComponent, ConstructionChangeEntityEvent>(OnCooked);
    }

    private void OnCooked(Entity<CFSpellSeasoningComponent> ent, ref ConstructionChangeEntityEvent args)
    {
        if (!ent.Comp.Magical || args.New == ent.Owner || !args.New.IsValid())
            return;

        var seasoning = EnsureComp<CFSpellSeasoningComponent>(args.New);
        seasoning.Magical = true;
        if (MetaData(args.New).EntityLifeStage >= EntityLifeStage.Initialized)
            Dirty(args.New, seasoning);
    }
}

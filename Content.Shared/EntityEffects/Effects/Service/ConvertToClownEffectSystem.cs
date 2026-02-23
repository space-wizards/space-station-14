using Content.Shared.Abilities.Mime;
using Content.Shared.Clothing.Systems;
using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Speech.Muting;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Service.Effects;

/// <summary>
/// Converts the target into a clown, making them clumsy and adding unremovable clown clothes to them.
/// Spares those who are already clumsy, muted, or have mime powers (can't be both a clown and a mime).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class ConvertToClownEffectSystem : EntityEffectSystem<MetaDataComponent, ConvertToClown>
{
    [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedOutfitSystem _outfitSystem = default!;
    [Dependency] private readonly SharedImplanterSystem _implanterSystem = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ConvertToClown> args)
    {
        if (!(HasComp<ClumsyComponent>(entity) || HasComp<MutedComponent>(entity) || HasComp<MimePowersComponent>(entity)))
        {
            AddComp<ClumsyComponent>(entity);

            _outfitSystem.SetOutfit(entity.Owner, args.Effect.OutfitId, unremovable: true, stripEmptySlots: false, respectEquippability: true);

            var transformMessage = Loc.GetString(args.Effect.ConvertMessage, ("target", entity.Owner));
            _sharedPopupSystem.PopupEntity(transformMessage, entity.Owner, PopupType.LargeCaution);

            _damageableSystem.TryChangeDamage(entity.Owner, args.Effect.ConvertDamage);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ConvertToClown : EntityEffectBase<ConvertToClown>
{
    /// <summary>
    /// Outfit ID for the "clothing" the target grows
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> OutfitId = "ConvertedClownGear";

    /// <summary>
    /// ID for the sad trombone implant
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> ImplantId = "ConvertedClownGear";

    /// <summary>
    /// Message to popup when succesfully converting
    /// </summary>
    [DataField]
    public LocId ConvertMessage = "clown-conversion";

    /// <summary>
    /// Amount of cellular damage dealt when succesfully converting
    /// </summary>
    [DataField]
    public DamageSpecifier ConvertDamage = new()
    {
        DamageDict = new()
        {
            { "Cellular", 15.0 },
        },
    };
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-clown-conversion", ("chance", Probability));
}

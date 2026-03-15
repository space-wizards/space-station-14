using Content.Shared.Abilities.Mime;
using Content.Shared.Alert;
using Content.Shared.Clothing.Systems;
using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Speech.Muting;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Service.Effects;

/// <summary>
/// Converts the target into a mime, making them mute and adding unremovable mime clothes to them (but doesn't give them mime powers).
/// Spares those who are already clumsy, muted, or have mime powers (can't be both a clown and a mime).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class ConvertToMimeEffectSystem : EntityEffectSystem<MetaDataComponent, ConvertToMime>
{
    [Dependency] private readonly SharedPopupSystem _sharedPopupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedOutfitSystem _outfitSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ConvertToMime> args)
    {
        if (!(HasComp<ClumsyComponent>(entity) || HasComp<MutedComponent>(entity) || HasComp<MimePowersComponent>(entity)))
        {
            AddComp<MutedComponent>(entity);

            _alertsSystem.ShowAlert(entity.Owner, args.Effect.SilencedAlert);

            _outfitSystem.SetOutfit(entity.Owner, args.Effect.OutfitId, unremovable: true, stripEmptySlots: false, respectEquippability: true);

            var transformMessage = Loc.GetString(args.Effect.ConvertMessage, ("target", entity.Owner));
            _sharedPopupSystem.PopupEntity(transformMessage, entity.Owner, PopupType.LargeCaution);

            _damageableSystem.TryChangeDamage(entity.Owner, args.Effect.ConvertDamage);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ConvertToMime : EntityEffectBase<ConvertToMime>
{
    /// <summary>
    /// Outfit ID for the "clothing" the target grows
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> OutfitId = "ConvertedMimeGear";

    /// <summary>
    /// Alert to tell the target they have been permanently silenced
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> SilencedAlert = "Silenced";

    /// <summary>
    /// Message to popup when succesfully converting
    /// </summary>
    [DataField]
    public LocId ConvertMessage = "mime-conversion";

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
        => Loc.GetString("entity-effect-guidebook-mime-conversion", ("chance", Probability));
}

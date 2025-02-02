using Content.Shared.Item.ItemToggle.Components;
using Content.Shared._Harmony.Clothing.Gloves.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._Harmony.Clothing.Gloves.Systems;

/// <summary>
/// Handles the activation and deactivation of mantis gloves,
/// including their visual state and metadata changes.
/// </summary>
public abstract class SharedMantisGlovesSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MantisGlovesComponent, ItemToggledEvent>(OnToggled);
    }

    /// <summary>
    /// Handles the toggling of mantis gloves between activated and deactivated states.
    /// </summary>
    private void OnToggled(EntityUid uid, MantisGlovesComponent component, ref ItemToggledEvent args)
    {
        UpdateMantisGlovesState(uid, args.Activated, component);

        if (!TryComp<ClothingComponent>(uid, out var clothing))
            return;

        // Update the visual state of the gloves
        _clothing.SetEquippedPrefix(uid, args.Activated ? "activated" : null, clothing);

        if (args.User != null)
        {
            var message = args.Activated ? Loc.GetString(component.ActivatedPopUp!) : Loc.GetString(component.DeactivatedPopUp!);
            _popup.PopupClient(message, uid, args.User.Value);
        }
    }

    /// <summary>
    /// Updates the metadata (name and description) of mantis gloves based on their activation state.
    /// When activated, uses the localization keys from the component
    /// When deactivated, uses the prototype values but wraps them in Loc.GetString
    /// </summary>
    private void UpdateMantisGlovesState(EntityUid uid, bool activated, MantisGlovesComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var meta = MetaData(uid);
        var protoId = meta.EntityPrototype?.ID;

        if (protoId == null)
            return;

        if (!_prototypeManager.TryIndex<EntityPrototype>(protoId, out var prototype))
            return;

        string name = activated
            ? Loc.GetString(component.ActivatedName!)
            : prototype.Name;

        string description = activated
            ? Loc.GetString(component.ActivatedDescription!)
            : prototype.Description;

        _metaData.SetEntityName(uid, name);
        _metaData.SetEntityDescription(uid, description);
    }
}

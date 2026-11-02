using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CF.Alchemy;

/// <summary>
/// Shared data for Pathfinder alchemical / brew consumables (elixirs, potions, etc.).
/// Traits live on <c>Tag</c> (e.g. CFConsumable, CFHealing, CFPotion).
/// Drink timing/sound mirrors SS14 <c>Edible</c> drinks (e.g. Space Cola).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFAlchemicalItemComponent : Component
{
    /// <summary>PF2e item level.</summary>
    [DataField, AutoNetworkedField]
    public int ItemLevel = 1;

    /// <summary>Price in gold pieces (for display / economy later).</summary>
    [DataField, AutoNetworkedField]
    public int PriceGp;

    /// <summary>If true, the item is destroyed after a successful activation.</summary>
    [DataField, AutoNetworkedField]
    public bool Consumable = true;

    /// <summary>Held-in-hand bulk hint (L = light).</summary>
    [DataField, AutoNetworkedField]
    public string Bulk = "L";

    /// <summary>Do-after length when drinking yourself (progress bar). Same default as <c>EdibleComponent.Delay</c>.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>Do-after when force-feeding another (progress bar). Same default as <c>EdibleComponent.ForceFeedDelay</c>.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ForceFeedDelay = TimeSpan.FromSeconds(3);

    /// <summary>Played on successful drink — Space Cola / Drink edible uses drink.ogg.</summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithVariation(0.25f),
    };
}

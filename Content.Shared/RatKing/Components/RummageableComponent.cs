using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.RatKing.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.RatKing.Components;

/// <summary>
/// This is used for entities that can be
/// rummaged through by the rat king to get loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RummagerSystem))]
[AutoGenerateComponentState]
public sealed partial class RummageableComponent : Component
{
    /// <summary>
    /// Whether or not this entity has been rummaged through already.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Looted;

    /// <summary>
    /// How long it takes to rummage through a rummageable container.
    /// </summary>
    [DataField]
    public float RummageDuration = 3f;

    /// <summary>
    /// The entity table to select loot from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Sound played on rummage completion.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("storageRustle");
}

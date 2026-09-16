using Content.Shared.Tabletop.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TabletopDimension : EntityEffectBase<TabletopDimension>
{
    /// <summary>
    /// Tabletop game entity prototype whose board this entity is banished to.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<TabletopGameComponent> Prototype;
}

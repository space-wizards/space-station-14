using Content.Shared.Tabletop.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TabletopDimension : EntityEffectBase<TabletopDimension>
{
    [DataField(required: true)]
    public EntProtoId<TabletopGameComponent> Prototype;
}

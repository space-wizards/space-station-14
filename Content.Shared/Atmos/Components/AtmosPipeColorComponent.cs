using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;
using JetBrains.Annotations;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AtmosPipeColorComponent : Component
{
    [DataField("color"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Color Color { get; set; } = Color.White;

    [ViewVariables(VVAccess.ReadWrite), UsedImplicitly]
    public Color ColorVV
    {
        get => Color;
        set => IoCManager.Resolve<IEntityManager>().System<AtmosPipeColorSystem>().SetColor((Owner, this), value);
    }
}

[ByRefEvent]
public record struct AtmosPipeColorChangedEvent(Color color)
{
    public Color Color = color;
}

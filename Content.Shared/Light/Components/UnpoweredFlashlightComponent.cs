using Content.Shared.Decals;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Light.Components;

/// <summary>
/// This is simplified version of <see cref="HandheldLightComponent"/>.
/// It doesn't consume any power and can be toggle only by verb.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnpoweredFlashlightComponent : Component
{
    [DataField("toggleFlashlightSound")]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");

    [DataField("lightOn"), AutoNetworkedField]
    public bool LightOn = false;

    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleLight";

    [DataField("toggleActionEntity"), AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    ///  <see cref="ColorPalettePrototype"/> ID that determines the list
    /// of colors to select from when we get emagged
    /// </summary>
    [DataField("emaggedColorsPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<ColorPalettePrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string EmaggedColorsPrototype = "Emagged";
}

using Content.Server.Ame.EntitySystems;
using Content.Shared.Ame.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Ame.Components;

/// <summary>
/// The component used to make an entity the controller/fuel injector port of an AntiMatter Engine.
/// Connects to adjacent entities with this component or <see cref="AmeShieldComponent"/> to make an AME.
/// </summary>
[Access(typeof(AmeControllerSystem), typeof(AmeNodeGroup))]
[RegisterComponent]
public sealed partial class AmeControllerComponent : SharedAmeControllerComponent
{
    /// <summary>
    /// Antimatter fuel slot.
    /// </summary>
    [DataField("fuelSlot")]
    [ViewVariables(VVAccess.ReadWrite)]
    public ItemSlot FuelSlot = new();

    /// <summary>
    /// Whether or not the AME controller is currently injecting animatter into the reactor.
    /// </summary>
    [DataField("injecting")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Injecting = false;

    /// <summary>
    /// How much antimatter the AME controller is set to inject into the reactor per update.
    /// </summary>
    [DataField("injectionAmount")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int InjectionAmount = 2;

    /// <summary>
    /// How stable the reactor currently is.
    /// When this falls to <= 0 the reactor explodes.
    /// </summary>
    [DataField("stability")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Stability = 100;

    /// <summary>
    /// The sound used when pressing buttons in the UI.
    /// </summary>
    [DataField("clickSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// The sound used when injecting antimatter into the AME.
    /// </summary>
    [DataField("injectSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier InjectSound = new SoundCollectionSpecifier("MetalThud");

    /// <summary>
    /// The last time this could have injected fuel into the AME.
    /// </summary>
    [DataField("lastUpdate")]
    public TimeSpan LastUpdate = default!;

    /// <summary>
    /// The next time this will try to inject fuel into the AME.
    /// </summary>
    [DataField("nextUpdate")]
    public TimeSpan NextUpdate = default!;

    /// <summary>
    /// The next time this will try to update the controller UI.
    /// </summary>
    public TimeSpan NextUIUpdate = default!;

    /// <summary>
    /// The the amount of time that passes between injection attempts.
    /// </summary>
    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UpdatePeriod = TimeSpan.FromSeconds(10.0);

    /// <summary>
    /// The maximum amount of time that passes between UI updates.
    /// </summary>
    [ViewVariables]
    public TimeSpan UpdateUIPeriod = TimeSpan.FromSeconds(3.0);

    /// <summary>
    /// Time at which the admin alarm sound effect can next be played.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EffectCooldown;

    /// <summary>
    /// Time between admin alarm sound effects. Prevents spam
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(10f);
}

using Content.Server.Ame.EntitySystems;
using Content.Shared.Ame;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

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
    /// The id of the container used to store the current fuel container for the AME.
    /// </summary>
    public const string FuelContainerId = "AmeFuel";

    /// <summary>
    /// The container for the fuel canisters used by the AME.
    /// </summary>
    [ViewVariables]
    public ContainerSlot JarSlot = default!;

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
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Effects/bang.ogg");

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
    /// The the amount of time that passes between injection attempts.
    /// </summary>
    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UpdatePeriod = TimeSpan.FromSeconds(10.0);
}

using Content.Server.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Body.Components;

[RegisterComponent]
[Access(typeof(ThermalRegulatorSystem))]
public sealed partial class ThermalRegulatorComponent : Component
{
    /// <summary>
    /// The next time that the body will regulate its heat.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval at which thermal regulation is processed.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Heat generated due to metabolism. It's generated via metabolism
    /// </summary>
    [DataField]
    public float MetabolismHeat;

    /// <summary>
    /// Heat output via radiation.
    /// </summary>
    [DataField]
    public float RadiatedHeat;

    /// <summary>
    /// Maximum heat regulated via sweat
    /// </summary>
    [DataField]
    public float SweatHeatRegulation;

    /// <summary>
    /// Maximum heat regulated via shivering
    /// </summary>
    [DataField]
    public float ShiveringHeatRegulation;

    /// <summary>
    /// Amount of heat regulation that represents thermal regulation processes not
    /// explicitly coded.
    /// </summary>
    [DataField]
    public float ImplicitHeatRegulation;

    /// <summary>
    /// Normal body temperature
    /// </summary>
    [DataField]
    public float NormalBodyTemperature;

    /// <summary>
    /// Deviation from normal temperature for body to start thermal regulation
    /// </summary>
    [DataField]
    public float ThermalRegulationTemperatureThreshold;

    /// <summary>
    ///     The emote that shows when an entity sweats
    /// </summary>
    [DataField]
    public ProtoId<EmotePrototype> SweatEmote = "Sweat";

    /// <summary>
    ///     The emote that shows when an entity shivers
    /// </summary>
    [DataField]
    public ProtoId<EmotePrototype> ShiverEmote = "Shiver";

    /// <summary>
    ///     The maximum time between each sweat or shiver emote
    /// </summary>
    [DataField]
    public TimeSpan EmoteCooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public float SweatEmoteProgress;

    [DataField]
    public float ShiverEmoteProgress;

    /// <summary>
    ///     Does this entity do the sweat emote when warm
    /// </summary>
    [DataField]
    public bool VisuallySweats;

    /// <summary>
    ///     Does this entity do the shiver emote when cold
    /// </summary>
    [DataField]
    public bool VisuallyShivers;
}

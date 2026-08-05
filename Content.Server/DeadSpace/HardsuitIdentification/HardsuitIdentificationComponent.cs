// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.HardsuitIdentification;

[RegisterComponent]
public sealed partial class HardsuitIdentificationComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionHardsuitSaveDNA";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public string DNA = String.Empty;

    [DataField]
    public bool DNAWasStored = false;

    [DataField]
    public bool Activated = false;

    [DataField]
    public EntProtoId PunishmentImplant = "DeathAcidifierImplant";

    public EntityUid? PunishmentImplantEntity;

    public bool PunishmentTriggered;

    /// <summary>
    /// Whether the wearer should be dissolved if their DNA changes or they transform into an undead creature.
    /// </summary>
    [DataField]
    public bool DissolveOnDnaChange = true;

    /// <summary>
    /// Emag sound effects.
    /// </summary>
    [DataField]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8),
    };

    /// <summary>
    /// Sound played when a non-owner tries to equip the item.
    /// </summary>
    [DataField]
    public SoundSpecifier WrongOwnerSound = new SoundPathSpecifier("/Audio/Effects/multitool_pulse.ogg")
    {
        Params = AudioParams.Default.WithVolume(4),
    };

    [DataField]
    public bool CanEmag = true;

    [DataField]
    public bool Nonlethal;
}

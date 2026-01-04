using Content.Server.Botany.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Plays a screams when the plant is harvested.
/// </summary>
[DataDefinition]
public sealed partial class TraitScream : PlantTrait
{
    /// <summary>
    /// The sound to play when the plant screams.
    /// </summary>
    [DataField]
    public SoundSpecifier ScreamSound = new SoundCollectionSpecifier("PlantScreams");

    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void OnAfterDoHarvest(Entity<PlantTraitsComponent> ent, ref AfterDoHarvestEvent args)
    {
        _audio.PlayPvs(ScreamSound, ent.Owner);
    }

    public override IEnumerable<string> GetPlantStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-scream");
    }
}

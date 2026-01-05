using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Botany.Systems;

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
        _audio.PlayPredicted(ScreamSound, ent, args.User);
    }

    public override IEnumerable<string> GetTraitStateMarkup()
    {
        yield return Loc.GetString("mutation-plant-scream");
    }
}

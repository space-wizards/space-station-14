using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emag.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.Clothing.Systems;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, HailerOrderMessage>(OnHailOrder);
    }

    private void OnHailOrder(EntityUid uid, HailerComponent comp, HailerOrderMessage args)
    {
        string soundCollection;
        string localeText;
        Entity<HailerComponent> ent = (uid, comp);

        if (HasComp<EmaggedComponent>(ent) && comp.EmagLevelPrefix != null)
        {
            //Emag lines
            localeText = soundCollection = comp.EmagLevelPrefix;
        }
        else
        {
            //Set the strings needed for choosing a file in the SoundCollection and the corresponding loc string
            var orderUsed = comp.Orders[args.OrderIndex];
            var hailLevel = comp.CurrentHailLevel != null ? "-" + comp.CurrentHailLevel.Value.Name : String.Empty;
            soundCollection = orderUsed.SoundCollection + hailLevel;
            localeText = orderUsed.LocalePrefix + hailLevel;
        }

        //Play voice audio line and we get the index of the randomly choosen sound in the SoundCollection
        var index = PlayVoiceLineSound((uid, comp), soundCollection);

        //Send chat message, based on the index of the audio file to match with the loc string
        SubmitChatMessage((uid, comp), localeText, index);
    }

    /// <summary>
    /// Play an audio of a voice line
    /// Chooses randomly a sound from the given SoundCollection and return the index
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="soundCollection">Choosen SoundCollection</param>
    /// <returns>Randomly choosen index of the played audio sound in the SoundCollection</returns>
    private int PlayVoiceLineSound(Entity<HailerComponent> ent, string soundCollection)
    {
        var specifier = new SoundCollectionSpecifier(soundCollection);
        var resolver = _audio.ResolveSound(specifier); //Since this uses Robust Random, this can't be predicted
        if (resolver is ResolvedCollectionSpecifier collectionResolver)
        {
            _audio.PlayPvs(resolver, ent.Owner, audioParams: new AudioParams().WithVolume(-3f));
            return collectionResolver.Index;
        }
        else
            Log.Error("SharedHailerSystem tried to play an audio file NOT from a SoundCollection !");
        return -1;
    }
}

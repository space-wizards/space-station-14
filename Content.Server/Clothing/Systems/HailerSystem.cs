using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emag.Components;
using Content.Shared.Timing;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.Clothing.Systems;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, HailerOrderMessage>(OnHailOrder);
    }

    private void OnHailOrder(Entity<HailerComponent> ent, ref HailerOrderMessage args)
    {
        if (TryComp<UseDelayComponent>(ent.Owner, out var delay))
        {
            _delay.TryResetDelay(ent.Owner);
        }

        string soundCollection;
        string localeText;

        if (HasComp<EmaggedComponent>(ent) && ent.Comp.EmagLevelPrefix != null)
        {
            //Emag lines
            localeText = soundCollection = ent.Comp.EmagLevelPrefix;
        }
        else
        {
            //Set the strings needed for choosing a file in the SoundCollection and the corresponding loc string
            var orderUsed = ent.Comp.Orders[args.OrderIndex];
            var hailLevel = ent.Comp.CurrentHailLevel != null ? "-" + ent.Comp.CurrentHailLevel.Value.Name : String.Empty;
            soundCollection = orderUsed.SoundCollection + hailLevel;
            localeText = orderUsed.LocalePrefix + hailLevel;
        }

        //Play voice audio line and we get the index of the randomly choosen sound in the SoundCollection
        var index = PlayVoiceLineSound(ent, soundCollection);

        //Send chat message, based on the index of the audio file to match with the loc string
        SubmitChatMessage(ent, localeText, index);
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

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
    }

    protected override void OnHailOrder(Entity<HailerComponent> ent, ref HailerOrderMessage args)
    {
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

    private int PlayVoiceLineSound(Entity<HailerComponent> ent, string soundCollection)
    {
        var specifier = new SoundCollectionSpecifier(soundCollection);
        var resolver = _audio.ResolveSound(specifier);
        if (resolver is ResolvedCollectionSpecifier collectionResolver)
        {
            _audio.PlayPvs(resolver, ent.Owner, audioParams: new AudioParams().WithVolume(-3f)); //PlayPredicted doesnt support playing a resolved SoundCollection sound
            return collectionResolver.Index;
        }
        else
            Log.Error("SharedHailerSystem tried to play an audio file NOT from a SoundCollection !");
        return -1;
    }
}

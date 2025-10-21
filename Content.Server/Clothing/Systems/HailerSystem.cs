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
            localeText = soundCollection = comp.EmagLevelPrefix;
        }
        else
        {
            var orderUsed = comp.Orders[args.Index];
            var hailLevel = comp.CurrentHailLevel != null ? "-" + comp.CurrentHailLevel.Value.Name : String.Empty;
            soundCollection = orderUsed.SoundCollection + hailLevel;
            localeText = orderUsed.LocalePrefix + hailLevel;
        }

        //Play voice etc...
        var index = PlayVoiceLineSound((uid, comp), soundCollection);
        SubmitChatMessage((uid, comp), localeText, index);
    }

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

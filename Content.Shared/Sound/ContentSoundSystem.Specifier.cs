using Content.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Sound;

public sealed class ContentSoundSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public string GetRandomFileFromSoundCollection(string name)
    {
        var soundCollection = _proto.Index<SoundCollectionPrototype>(name);
        return _random.Pick(soundCollection.PickFiles).ToString();
    }

    public string GetSound(SoundSpecifier specifier)
    {
        switch (specifier)
        {
            case SoundCollectionSpecifier collection:
                return collection.Collection == null ? string.Empty : GetRandomFileFromSoundCollection(collection.Collection);
            case SoundPathSpecifier path:
                return path.Path == null ? string.Empty : path.Path.ToString();
            default:
                throw new NotImplementedException();
        }
    }
}

using Content.Server.CharacterAppearance.Components;
using Content.Shared.Preferences;

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidSystem _humanoid = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        var profile = HumanoidCharacterProfile.Random();
        _humanoid.LoadProfile(uid, profile);

        if (component.RandomizeName)
        {
            var meta = MetaData(uid);
            meta.EntityName = profile.Name;
        }
    }
}

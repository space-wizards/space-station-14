using Content.Server.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;

namespace Content.Server.CharacterAppearance.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
        {
            var profile = HumanoidCharacterProfile.Random();
            _humanoidAppearance.UpdateFromProfile(uid, profile, appearance);

            if (component.RandomizeName)
            {
                var meta = MetaData(uid);
                meta.EntityName = profile.Name;
            }
        }
    }
}

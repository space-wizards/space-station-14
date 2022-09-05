using Content.Server.CharacterAppearance.Components;
using Content.Server.IdentityManagement;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;

namespace Content.Server.CharacterAppearance.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, RandomHumanoidAppearanceComponent component, ComponentStartup args)
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
        _identity.QueueIdentityUpdate(uid);
    }
}

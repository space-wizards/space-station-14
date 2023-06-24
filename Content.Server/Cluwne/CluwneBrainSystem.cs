using Content.Shared.Cluwne;
using Content.Shared.Humanoid;
using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction.Components;

namespace Content.Server.Cluwne;

public sealed class CluwneBrainSystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CluwneBrainComponent, ComponentStartup>(AfterEat);
    }

    private void AfterEat(EntityUid uid, CluwneBrainComponent component, ComponentStartup args)
    {
        if (HasComp<HumanoidAppearanceComponent>(uid)
            && HasComp<CluwneComponent>(uid)
            && !HasComp<ZombieComponent>(uid))
        {
            _audio.PlayPvs(component.HonkSound, uid);
            _polymorph.PolymorphEntity(uid, "ForcedCluwneBeast");
            RemComp<CluwneBrainComponent>(uid);
        }

        else if (HasComp<HumanoidAppearanceComponent>(uid)
            && !HasComp<CluwneComponent>(uid)
            && !HasComp<ClumsyComponent>(uid)
            && !HasComp<ZombieComponent>(uid))
        {
            EnsureComp<CluwneComponent>(uid);
            RemComp<CluwneBrainComponent>(uid);
        }
    }
}

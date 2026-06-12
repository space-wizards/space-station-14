using Content.Shared.Random.Helpers;
using Content.Shared._Offbrand.Medical;
using Content.Shared._Offbrand.Wounds;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Organs;

public sealed partial class OffbrandDamageableOrganSoundsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OffbrandDamageableOrganSoundsComponent, StethoscopeExamineEvent>(OnStethoscopeExamine);
    }

    private void OnStethoscopeExamine(Entity<OffbrandDamageableOrganSoundsComponent> ent, ref StethoscopeExamineEvent args)
    {
        var damage = Comp<DamageableOrganComponent>(ent);
        if (ent.Comp.Descriptions.HighestMatch(damage.Damage) is not { } match)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new RobustRandom();
        rand.SetSeed(seed);

        var line = rand.Pick(_prototype.Index(match));
        args.Messages.Add(line);
    }
}

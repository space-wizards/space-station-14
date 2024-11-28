using Content.Shared.Abilities.Goliath;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.UserInterface;
using Robust.Shared.Player;

namespace Content.Shared._Starlight.Antags.TerrorSpider;
public sealed class TerrorEggSystem : AccUpdateEntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private readonly Dictionary<EntityUid, Entity<EggHolderComponent>> _eggs = [];
    private readonly EntProtoId[] _terrorSpiders = ["MobTerrorGray", "MobTerrorGreen", "MobTerrorRed"];
    private DamageTypePrototype? _blunt;
    private DamageSpecifier? _damage;
    public override void Initialize()
    {
        SubscribeLocalEvent<EggHolderComponent, ComponentInit>(AddEgg);
        SubscribeLocalEvent<EggHolderComponent, ComponentShutdown>(RemoveEgg);
    }

    private void AddEgg(Entity<EggHolderComponent> ent, ref ComponentInit args) => _eggs.TryAdd(ent.Owner, ent);
    private void RemoveEgg(Entity<EggHolderComponent> ent, ref ComponentShutdown args) => _eggs.Remove(ent.Owner);

    protected override float Threshold { get; set; } = 1f;
    protected override void AccUpdate()
    {
        foreach (var egg in _eggs)
        {
            egg.Value.Comp.Counter++;
            _blunt ??= _prototypes.Index<DamageTypePrototype>("Blunt");
            _damage ??= new(_blunt, 1);
            _damageable.TryChangeDamage(egg.Value.Owner, _damage, false);
            if (egg.Value.Comp.Counter >= 300)
            {
                var entity = EntityManager.SpawnEntity(_random.Pick(_terrorSpiders), Transform(egg.Value.Owner).Coordinates);
                RemComp<EggHolderComponent>(egg.Value.Owner);
            }
        }
    }
}
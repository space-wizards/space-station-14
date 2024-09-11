using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Server.Temperature.Systems;
using Content.Shared.Atmos;
using Content.Shared.Popups;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Heretic;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Server.Temperature.Components;
using Content.Server.Body.Components;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticCombatMarkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public bool ApplyMarkEffect(EntityUid target, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        switch (path)
        {
            case "Ash":
                // gives fire stacks
                _flammable.AdjustFireStacks(target, 5, ignite: true);
                break;

            case "Blade":
                // TODO: add rotating protective blade type shit
                break;

            case "Flesh":
                if (TryComp<BloodstreamComponent>(target, out var blood))
                {
                    _blood.TryModifyBleedAmount(target, 5f, blood);
                    _blood.SpillAllSolutions(target, blood);
                }
                break;

            case "Lock":
                // bolts nearby doors
                var lookup = _lookup.GetEntitiesInRange(target, 5f);
                foreach (var door in lookup)
                {
                    if (!TryComp<DoorBoltComponent>(door, out var doorComp))
                        continue;
                    _door.SetBoltsDown((door, doorComp), true);
                }
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/knock.ogg"), target);
                break;

            case "Rust":
                // TODO: add item damage, for now just break a random item
                if (!TryComp<InventoryComponent>(target, out var inv))
                    break;

                var contrandom = _random.Next(0, inv.Containers.Length - 1);
                if (contrandom < 0)
                    break;
                var cont = inv.Containers[contrandom];

                var itemrandom = _random.Next(0, cont.ContainedEntities.Count - 1);
                if (itemrandom < 0)
                    break;
                var item = cont.ContainedEntities[itemrandom];

                _popup.PopupEntity(Loc.GetString("heretic-rust-mark-itembreak", ("name", Name(item))), target, PopupType.LargeCaution);
                QueueDel(item);
                break;

            case "Void":
                // set target's temperature to -40C
                // is really OP with the new temperature slowing thing :godo:
                if (TryComp<TemperatureComponent>(target, out var temp))
                    _temperature.ForceChangeTemperature(target, temp.CurrentTemperature - 100f, temp);
                break;

            default:
                return false;
        }

        // transfers the mark to the next nearby person
        var look = _lookup.GetEntitiesInRange(target, 2.5f);
        if (look.Count != 0)
        {
            var lookent = look.ToArray()[0];
            if (HasComp<HumanoidAppearanceComponent>(lookent)
            && !HasComp<HereticComponent>(lookent))
            {
                var markComp = EnsureComp<HereticCombatMarkComponent>(lookent);
                markComp.Path = path;
            }
        }

        return true;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentStartup>(OnStart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var comp in EntityQuery<HereticCombatMarkComponent>())
        {
            if (_timing.CurTime > comp.Timer)
                RemComp(comp.Owner, comp);
        }
    }

    private void OnStart(Entity<HereticCombatMarkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Timer == TimeSpan.Zero)
            ent.Comp.Timer = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.DisappearTime);
    }
}

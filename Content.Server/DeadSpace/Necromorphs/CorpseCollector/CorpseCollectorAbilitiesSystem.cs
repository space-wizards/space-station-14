// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Shared.DeadSpace.Necromorphs.CorpseCollector;
using Content.Shared.DeadSpace.Necromorphs.CorpseCollector.Components;
using Content.Shared.Inventory;
using Content.Server.Inventory;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Content.Shared.Damage;

namespace Content.Server.DeadSpace.Necromorphs;

public sealed class CorpseCollectorAbilitiesSystem : SharedCorpseCollectorSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorpseCollectorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CorpseCollectorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CorpseCollectorComponent, AbsorptionDeadNecroActionEvent>(DoAbsorption);
        SubscribeLocalEvent<CorpseCollectorComponent, SpawnPointNecroActionEvent>(DoSpawn);
        SubscribeLocalEvent<CorpseCollectorComponent, AbsorptionDeadNecroDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CorpseCollectorComponent, SpawnLeviathanActionEvent>(OnSpawnLeviathan);
    }
    private void OnComponentInit(EntityUid uid, CorpseCollectorComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.ActionAbsorptionDeadNecroEntity, component.ActionAbsorptionDeadNecro, uid);
        _actions.AddAction(uid, ref component.ActionSpawnPointEntity, component.ActionSpawnPointNecro, uid);
    }

    private void OnShutdown(EntityUid uid, CorpseCollectorComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionAbsorptionDeadNecroEntity);
        _actions.RemoveAction(uid, component.ActionSpawnPointEntity);
        _actions.RemoveAction(uid, component.ActionSpawnLeviathanEntity);
    }
    private void OnSpawnLeviathan(EntityUid uid, CorpseCollectorComponent component, SpawnLeviathanActionEvent args)
    {
        if (args.Handled)
            return;

        var tileref = Transform(uid).Coordinates.GetTileRef();
        if (tileref != null)
        {
            if (_physics.GetEntitiesIntersectingBody(uid, (int)CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return;
            }
        }

        args.Handled = true;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return;

        var ent = Spawn(component.LeviathanId, Transform(uid).Coordinates);

        if (!EntityManager.TryGetComponent<GhostRoleComponent>(ent, out var ghostRoleComponent))
        {
            _mindSystem.TransferTo(mindId, ent);
            QueueDel(uid);
            return;
        }

        var id = ghostRoleComponent.Identifier;
        var session = mind.Session;

        if (session != null)
        {
            EntityManager.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(session, id);
        }
        else
        {
            return;
        }

        QueueDel(uid);
    }
    private void DoAbsorption(EntityUid uid, CorpseCollectorComponent component, AbsorptionDeadNecroActionEvent args)
    {
        if (args.Handled)
            return;

        CheckAbsorption(uid, component, args.Target);
        args.Handled = true;
    }

    private void DoSpawn(EntityUid uid, CorpseCollectorComponent component, SpawnPointNecroActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        if (component.CountAbsorptions <= 0)
        {
            _popup.PopupEntity(Loc.GetString("У вас недостаточно симбиотов для этого"), uid, uid);
            return;
        }

        args.Handled = true;

        SpawnPointNecro(component, Transform(uid).Coordinates);
        component.CountNecroDoDebuff += 1;
        if (component.CountNecroDoDebuff >= component.CountNecroDoDebuffMax)
        {
            DoDebuff(uid, component);
            component.CountNecroDoDebuff = 0;
        }
        _popup.PopupEntity(Loc.GetString($"Количество некроморфов = {component.CountAbsorptions}"), uid, uid);

        _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", uid, AudioParams.Default.WithVolume(3).WithMaxDistance(2f));
    }

    private void SpawnPointNecro(CorpseCollectorComponent component, EntityCoordinates coordinates)
    {
        if (component.MobIds.Length != component.SpawnChances.Length || component.MobIds.Length == 0 || component.SpawnChances.Length == 0)
        {
            throw new ArgumentException("Invalid input arrays");
        }

        Random random = new Random();

        // Генерируем случайное число от 0 до 100
        float randomValue = (float)random.NextDouble() * 100f;

        float cumulativeChance = 0f;
        for (int i = 0; i < component.MobIds.Length; i++)
        {
            cumulativeChance += component.SpawnChances[i];

            // Проверяем, в какой диапазон попало случайное число
            if (randomValue <= cumulativeChance)
            {
                // Спауним моба с соответствующим ID
                Spawn(component.MobIds[i], coordinates);
                break;
            }
        }
    }
    private void CheckAbsorption(EntityUid uid, CorpseCollectorComponent component, EntityUid target)
    {
        if (!HasComp<NecromorfComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете поглотить не некроморфное существо."), uid, uid);
            return;
        }

        BeginAbsorption(uid, component, target);
    }

    private void BeginAbsorption(EntityUid uid, CorpseCollectorComponent component, EntityUid target)
    {
        if (component.CountAbsorptions >= component.MaxAbsorptions)
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете поглотить еще больше."), uid, uid);
            return;
        }
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.AbsorptionDuration, new AbsorptionDeadNecroDoAfterEvent(), uid, target: target)
        {
            BreakOnMove = true,
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, CorpseCollectorComponent component, AbsorptionDeadNecroDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
        {
            _popup.PopupEntity(Loc.GetString("Существо не найдено"), uid, uid);
            return;
        }

        if (_mobState.IsDead(args.Args.Target.Value))
        {
            _audio.PlayPvs("/Audio/Effects/Fluids/splat.ogg", args.Args.Target.Value, AudioParams.Default.WithVariation(1f).WithVolume(4f));
            Unequipment(args.Args.Target.Value);
            QueueDel(args.Args.Target.Value);
            args.Handled = true;
            DoBuff(uid, component);
            _popup.PopupEntity(Loc.GetString($"Некроморф поглащен, колличество симбиотов = {component.CountAbsorptions}"), uid, uid);
            return;
        }
        _popup.PopupEntity(Loc.GetString("Существо должно быть мертвым."), uid, uid);
        args.Handled = true;
    }

    private void Unequipment(EntityUid entity)
    {
        if (_inventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {

            foreach (var slot in slotDefinitions)
            {
                _inventorySystem.TryUnequip(entity, slot.Name, true, true);
            }
        }
    }

    private void DoBuff(EntityUid uid, CorpseCollectorComponent component)
    {
        component.CountAbsorptions += 1;

        UpdateState(uid, component);

        if (!EntityManager.TryGetComponent(uid, out MeleeWeaponComponent? weapon))
            return;

        weapon.Damage *= component.BuffDamage;

        component.MovementSpeedMultiplier *= component.BuffSpeed;
        _movement.RefreshMovementSpeedModifiers(uid);

        component.PassiveHealingMultiplier *= component.BuffHeal;
    }

    private void DoDebuff(EntityUid uid, CorpseCollectorComponent component)
    {
        component.CountAbsorptions -= 1;


        UpdateState(uid, component);

        if (!EntityManager.TryGetComponent(uid, out MeleeWeaponComponent? weapon))
            return;

        weapon.Damage /= component.BuffDamage;

        component.MovementSpeedMultiplier /= component.BuffSpeed;
        _movement.RefreshMovementSpeedModifiers(uid);

        component.PassiveHealingMultiplier /= component.BuffHeal;
    }

    private void UpdateState(EntityUid uid, CorpseCollectorComponent component)
    {
        float countAbsorptions = (float)component.CountAbsorptions;
        float shag = (float)component.MaxAbsorptions / 3;

        if (countAbsorptions < shag)
        {
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl2, false);
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl3, false);
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl1, true);
            _damage.SetDamageModifierSetId(uid, "CorpseCollectorLvl1");
            _actions.RemoveAction(uid, component.ActionSpawnLeviathanEntity);
        }
        if (countAbsorptions > shag && countAbsorptions < shag * 2)
        {
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl2, true);
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl3, false);
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl1, false);
            _damage.SetDamageModifierSetId(uid, "CorpseCollectorLvl2");
            _actions.RemoveAction(uid, component.ActionSpawnLeviathanEntity);
        }
        if (countAbsorptions > shag * 2)
        {
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl2, false);
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl3, true);
            _appearance.SetData(uid, CorpseCollectorVisuals.lvl1, false);
            _damage.SetDamageModifierSetId(uid, "CorpseCollectorLvl3");
            _actions.AddAction(uid, ref component.ActionSpawnLeviathanEntity, component.ActionSpawnLeviathan, uid);
        }
    }
}

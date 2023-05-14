using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.EstacaoPirata.Changeling;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Content.Server.EstacaoPirata.Changeling.Shop;
using Content.Server.Humanoid;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Actions;
using Content.Server.EstacaoPirata.Changeling.EntitySystems;
using Content.Server.Forensics;
using Content.Shared.DoAfter;
using Content.Server.Mind.Components;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using System.Linq;
using Content.Server.Inventory;
using Content.Server.Traitor;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Roles;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Server.Polymorph.Systems;
using Robust.Shared.Map;
using Robust.Server.Containers;

namespace Content.Server.EstacaoPirata.Changeling;
public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly ChangelingShopSystem _changShopSystem = default!;
    [Dependency] private readonly AbsorbSystem _absorbSystem = default!;
    //[Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly EntityManager _entMana = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedImplanterSystem _implanterSystem = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<ChangelingComponent, InstantActionEvent>(OnActionPerformed); // pra quando usar acao

        SubscribeLocalEvent<ChangelingComponent, AbsorbDNAActionEvent>(OnAbsorbAction);

        SubscribeLocalEvent<ChangelingComponent, AbsorbDNADoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<SubdermalImplantComponent, ChangelingShopActionEvent>(OnImplantShop);

        SubscribeLocalEvent<ChangelingComponent, ChangelingArmBladeEvent>(OnArmBlade);

        SubscribeLocalEvent<ChangelingComponent, ChangelingDnaStingEvent>(OnDnaSting);

        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformEvent>(OnTransform);

        // Initialize abilities
    }

    private void OnArmBlade(EntityUid uid, ChangelingComponent component, ChangelingArmBladeEvent args)
    {
        if (!TryComp<HandsComponent>(args.Performer, out var handsComponent))
            return;
        if (handsComponent.ActiveHand == null)
            return;

        var handContainer = handsComponent.ActiveHand.Container;

        if (handContainer == null)
            return;

        // checar se esta ativado
        //component.ArmBladeActivated = !component.ArmBladeActivated;


        // TODO: REFATORAR ISSO TUDO PRA USAR ArmBladeMaxHands, nao ficar spawnando e apagando entidade (usar o pause) e tambem fazer com que não se possa tirar o item da mão que está
        // esse codigo ta muito feio
        if (!component.ArmBladeActivated)
        {
            var targetTransformComp = Transform(args.Performer);
            var armbladeEntity = Spawn("TrueArmBlade", targetTransformComp.Coordinates);

            if (handContainer.ContainedEntity != null)
            {
                _handsSystem.TryDrop(args.Performer, handsComponent.ActiveHand, targetTransformComp.Coordinates);
            }

            _handsSystem.TryPickup(args.Performer, armbladeEntity);

            component.ArmBladeActivated = true;
        }
        else
        {
            if (handContainer.ContainedEntity != null)
            {
                if (TryPrototype(handContainer.ContainedEntity.Value, out var protoInHand))
                {
                    var result = _proto.HasIndex<EntityPrototype>("TrueArmBlade");
                    if (result)
                    {
                        EntityManager.DeleteEntity(handContainer.ContainedEntity.Value);
                        component.ArmBladeActivated = false;
                    }
                }
            }
        }
    }

    private void OnImplantShop(EntityUid uid, SubdermalImplantComponent component, ChangelingShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(args.Performer, uid, store);
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        //update the icon
        //ChangeEssenceAmount(uid, 0, component);

        //AbsorbDNA
        var absorbAction = new EntityTargetAction(_proto.Index<EntityTargetActionPrototype>("AbsorbDNA"))
            {
                CanTargetSelf = false
            };
        _action.AddAction(uid, absorbAction, null);

        // implante da loja
        var coords = Transform(uid).Coordinates;

        var implant = Spawn("ChangelingShopImplant", coords);

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
            return;

        _subdermalImplant.ForceImplant(uid, implant, implantComp);

        if (!TryComp<StoreComponent>(implant, out var storeComponent))
            return;

        storeComponent.Categories.Add("ChangelingAbilities");
        storeComponent.CurrencyWhitelist.Add("Points");

        storeComponent.Balance.Add(component.StoreCurrencyName,component.StartingPoints);

        // TODO: colocar cooldown?
        var dnaStingAction = new EntityTargetAction(_proto.Index<EntityTargetActionPrototype>("ChangelingDnaSting"))
            {
                CanTargetSelf = false
            };
        _action.AddAction(uid, dnaStingAction, null);

        var transformAction = new InstantAction(_proto.Index<InstantActionPrototype>("ChangelingTransform"));
        _action.AddAction(uid, transformAction, null);

        TryRegisterHumanoidData(uid, component);

        // Fazer isto em outro lugar

        // if (!TryComp<MindComponent>(uid, out var mindComp))
        //     return;
        // var themind = mindComp.Mind;
        // if (themind is null)
        //     return;
        // var role = new TraitorRole(themind, _proto.Index<AntagPrototype>("Changeling"));
        // themind.AddRole(role);


    }

    private void OnDoAfter(EntityUid uid, ChangelingComponent component, AbsorbDNADoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
                return;

        TryRegisterHumanoidData(uid, (EntityUid) args.Target,  component);
    }

    // Acho que nao vou usar este
    private void OnActionPerformed(EntityUid uid, ChangelingComponent component, InstantActionEvent args)
    {
    }

    private void OnAbsorbAction(EntityUid uid, ChangelingComponent component, AbsorbDNAActionEvent args)
    {
        StartAbsorbing(uid, args.Performer, args.Target, component);
    }

    private void StartAbsorbing(EntityUid scope, EntityUid user, EntityUid target, ChangelingComponent comp)
    {
        // se nao tiver mente, nao absorver
        // if(!HasComp<MindComponent>(target))
        //     return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-dna-failed-nonHumanoid"), user, user);
            return;
        }



        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(user, comp.AbsorbDNADelay, new AbsorbDNADoAfterEvent(), scope, target: target, used: scope)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true
        });
    }

    private void OnDnaSting(EntityUid uid, ChangelingComponent component, ChangelingDnaStingEvent args)
    {
        TryRegisterHumanoidData(uid, args.Target, component);
    }

    // register the user on startup
    private void TryRegisterHumanoidData(EntityUid uid, ChangelingComponent comp)
    {
        HumanoidData tempNewHumanoid = new HumanoidData();

        if (!TryComp<MetaDataComponent>(uid, out var targetMeta))
            return;
        if (!TryPrototype(uid, out var prototype, targetMeta))
            return;
        if (!TryComp<DnaComponent>(uid, out var dnaComp))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var targetHumAp))
            return;


        tempNewHumanoid.EntityPrototype = prototype;
        tempNewHumanoid.MetaDataComponent = targetMeta;
        tempNewHumanoid.AppearanceComponent = targetHumAp;
        tempNewHumanoid.Dna = dnaComp.DNA;
        tempNewHumanoid.EntityUid = uid;

        //Dirty(user, userMeta); // TENTANDO FAZER FICAR DIRTY

        if (comp.DNAStrandBalance >= comp.DNAStrandCap)
        {
            var lastHumanoidData = comp.StoredHumanoids.Last();
            comp.StoredHumanoids.Remove(lastHumanoidData);
            comp.StoredHumanoids.Add(tempNewHumanoid);
        }
        else
        {
            comp.DNAStrandBalance += 1;
            comp.StoredHumanoids.Add(tempNewHumanoid);
        }
    }

    private void TryRegisterHumanoidData(EntityUid user, EntityUid target, ChangelingComponent comp)
    {
        HumanoidData tempNewHumanoid = new HumanoidData();

        if (!TryComp<MetaDataComponent>(target, out var targetMeta))
            return;
        if (!TryPrototype(target, out var prototype, targetMeta))
            return;
        if (!TryComp<DnaComponent>(user, out var dnaComp))
        {
            _popup.PopupEntity(Loc.GetString("changeling-dna-failed-noDna"), user, user);
            return;
        }

        if (!TryComp<HumanoidAppearanceComponent>(target, out var targetHumAp))
        {
            _popup.PopupEntity(Loc.GetString("changeling-dna-failed-nonHumanoid"), user, user);
            return;
        }


        tempNewHumanoid.EntityPrototype = prototype;
        tempNewHumanoid.MetaDataComponent = targetMeta;
        tempNewHumanoid.AppearanceComponent = targetHumAp;
        tempNewHumanoid.Dna = dnaComp.DNA;

        var childUidNullable = SpawnPauseEntity(user, tempNewHumanoid, comp);

        if (childUidNullable == null)
            return;

        var childUid = (EntityUid) childUidNullable;

        tempNewHumanoid.EntityUid = childUid;

        //tempNewHumanoid.EntityUid = target; // erradissimo, n e pra eu pegar uid do target, mas sim do novo spawn

        //Dirty(user, userMeta); // TENTANDO FAZER FICAR DIRTY

        if (comp.DNAStrandBalance >= comp.DNAStrandCap)
        {
            var lastHumanoidData = comp.StoredHumanoids.Last();
            comp.StoredHumanoids.Remove(lastHumanoidData);
            comp.StoredHumanoids.Add(tempNewHumanoid);
        }
        else
        {
            comp.DNAStrandBalance += 1;
            comp.StoredHumanoids.Add(tempNewHumanoid);
        }



        _popup.PopupEntity(Loc.GetString("changeling-dna-obtained", ("target", target)), user, user);

        // Coisas para mover para outro metodo
        //userMeta.EntityName = targetMeta.EntityName;
        //_humanoidSystem.CloneAppearance(target, user, targetAppearance, userAppearance);

        // MELHOR: refatorar o MobChangeling, ao inves de fazer um mob especifico para ele, fazer um componente que diz se é ou não apenas
        // isso facilitaria a troca de corpo, pq ai só precisaria criar um novo mob identico ao alvo e passar o componente de changeling pra ele com os valores
        // if (TryPrototype(target, out var prototipo, targetMeta))
        // {
        //     var targetTransformComp = Transform(user);
        //     var child = Spawn(prototipo.ID, targetTransformComp.Coordinates);
        //
        // }
    }

    private void OnTransform(EntityUid uid, ChangelingComponent component, ChangelingTransformEvent args)
    {

        var storedHumanoids = component.StoredHumanoids;

        if (storedHumanoids.Count < 1)
            return;

        var firstHumanoid = storedHumanoids.First();
        if (firstHumanoid.EntityUid == uid)
            firstHumanoid = storedHumanoids.Last();
        if (firstHumanoid.EntityUid == uid)
            return;

        storedHumanoids.Remove(firstHumanoid);
        //var targetAppearance = firstHumanoid.AppearanceComponent;


        RetrievePausedEntity(uid, firstHumanoid, component);
    }

    // TODO: passar as actions compradas, quantia de dinheiro, itens na mao como o armblade
    private EntityUid? SpawnPauseEntity(EntityUid user, HumanoidData targetHumanoid, ChangelingComponent originalChangelingComponent)
    {
        if(targetHumanoid.EntityPrototype == null ||
           targetHumanoid.AppearanceComponent == null ||
           targetHumanoid.MetaDataComponent == null ||
           targetHumanoid.Dna == null
           )
            return null;

        var targetTransformComp = Transform(user);
        var child = Spawn(targetHumanoid.EntityPrototype.ID, targetTransformComp.Coordinates);

        targetHumanoid.EntityUid = child;

        var transformChild = Transform(child);
        transformChild.LocalRotation = targetTransformComp.LocalRotation;

        var childHumanoidAppearance = EnsureComp<HumanoidAppearanceComponent>(child);
        var childMeta = EnsureComp<MetaDataComponent>(child);
        var childDna = EnsureComp<DnaComponent>(child);

        var targetAppearance = targetHumanoid.AppearanceComponent;

        childHumanoidAppearance.Age = targetAppearance.Age;
        childHumanoidAppearance.BaseLayers = targetAppearance.BaseLayers;
        childHumanoidAppearance.CachedFacialHairColor = targetAppearance.CachedFacialHairColor;
        childHumanoidAppearance.CachedHairColor = targetAppearance.CachedHairColor;
        childHumanoidAppearance.CustomBaseLayers = targetAppearance.CustomBaseLayers;
        childHumanoidAppearance.EyeColor = targetAppearance.EyeColor;
        childHumanoidAppearance.Gender = targetAppearance.Gender;
        childHumanoidAppearance.HiddenLayers = targetAppearance.HiddenLayers;
        //childHumanoidAppearance.Initial = targetAppearance.Initial;
        childHumanoidAppearance.MarkingSet = targetAppearance.MarkingSet;
        childHumanoidAppearance.PermanentlyHidden = targetAppearance.PermanentlyHidden;
        childHumanoidAppearance.Sex = targetAppearance.Sex;
        childHumanoidAppearance.SkinColor = targetAppearance.SkinColor;
        childHumanoidAppearance.Species = targetAppearance.Species;


        childMeta.EntityName = targetHumanoid.MetaDataComponent.EntityName;
        childDna.DNA = targetHumanoid.Dna;

        // _inventory.TransferEntityInventories(user, child);
        // foreach (var hand in _hands.EnumerateHeld(user))
        // {
        //     _hands.TryDrop(user, hand, checkActionBlocker: false);
        //     _hands.TryPickupAnyHand(child, hand);
        // }

        var changelingComponent = EnsureComp<ChangelingComponent>(child);

        changelingComponent.ArmBladeActivated = originalChangelingComponent.ArmBladeActivated;
        changelingComponent.StoredHumanoids = originalChangelingComponent.StoredHumanoids;
        changelingComponent.DNAStrandBalance = originalChangelingComponent.DNAStrandBalance;
        changelingComponent.ChemicalBalance = originalChangelingComponent.ChemicalBalance;
        changelingComponent.PointBalance = originalChangelingComponent.PointBalance;
        changelingComponent.ArmBladeMaxHands = originalChangelingComponent.ArmBladeMaxHands;

        // EnsurePausesdMap();
        // if (PausedMap != null)
        // {
        //     _transform.SetParent(child, transformChild, PausedMap.Value);
        //     Logger.Info($"Entidade {child} spawnada e enviada para o mapa de pause {PausedMap.Value}");
        // }

        SendToPausesMap(child, transformChild);

        return child;

    }

    private void RetrievePausedEntity(EntityUid user, HumanoidData targetHumanoid, ChangelingComponent originalChangelingComponent)
    {
        // TODO: fazer com que spawne e resgate os personagens em pause no pausedmap

        var childNullable = targetHumanoid.EntityUid;

        if (childNullable == null)
            return;

        var child = (EntityUid) childNullable;

        if (Deleted(child))
            return;

        var childTransform = Transform(child);

        var userTransform = Transform(user);

        _transform.SetParent(child, childTransform, user);
        childTransform.Coordinates = userTransform.Coordinates; // ver esse negocio de obsoleto dps
        childTransform.LocalRotation = userTransform.LocalRotation;

        if (_container.TryGetContainingContainer(user, out var cont))
            cont.Insert(child);

        _inventory.TransferEntityInventories(user, child);
        foreach (var hand in _hands.EnumerateHeld(user))
        {
            _hands.TryDrop(user, hand, checkActionBlocker: false);
            _hands.TryPickupAnyHand(child, hand);
        }

        var childChangelingComponent = EnsureComp<ChangelingComponent>(child);

        //changelingComponent = comp;
        childChangelingComponent.ArmBladeActivated = originalChangelingComponent.ArmBladeActivated;
        childChangelingComponent.StoredHumanoids = originalChangelingComponent.StoredHumanoids;
        childChangelingComponent.DNAStrandBalance = originalChangelingComponent.DNAStrandBalance;
        childChangelingComponent.ChemicalBalance = originalChangelingComponent.ChemicalBalance;
        childChangelingComponent.PointBalance = originalChangelingComponent.PointBalance;
        childChangelingComponent.ArmBladeMaxHands = originalChangelingComponent.ArmBladeMaxHands;

        if (TryComp<DamageableComponent>(child, out var damageParent) &&
            _mobThreshold.GetScaledDamage(user, child, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage(child, damageParent, damage);
        }

        if (TryComp<MindComponent>(user, out var mind) && mind.Mind != null)
            mind.Mind.TransferTo(child);


        //EntityManager.DeleteEntity(user);

        SendToPausesMap(user, userTransform);

        Dirty(child);

        // criar gameobject com os atributos de HumanoidData

        //_humanoidSystem.CloneAppearance(target, user, targetAppearance, userAppearance);
    }

    private void SendToPausesMap(EntityUid uid, TransformComponent transform)
    {
        EnsurePausesdMap();

        if (PausedMap == null)
            return;

        _transform.SetParent(uid, transform, PausedMap.Value);
        Logger.Info($"Entidade {uid} spawnada e enviada para o mapa de pause {PausedMap.Value}");
    }
}

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
using Content.Server.Traitor;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Roles;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;

namespace Content.Server.EstacaoPirata.Changeling;
public sealed class ChangelingSystem : EntitySystem
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
        var absorbaction = new EntityTargetAction(_proto.Index<EntityTargetActionPrototype>("AbsorbDNA"))
            {
                CanTargetSelf = false
            };
        _action.AddAction(uid, absorbaction, null);

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
        var dnastingaction = new EntityTargetAction(_proto.Index<EntityTargetActionPrototype>("ChangelingDnaSting"))
            {
                CanTargetSelf = false
            };
        _action.AddAction(uid, dnastingaction, null);

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

    private void ChangeAppearance(EntityUid user, HumanoidAppearanceComponent userAppearance, EntityUid target, HumanoidAppearanceComponent targetAppearance, ChangelingComponent comp)
    {
        // criar gameobject com os atributos de HumanoidData

        //_humanoidSystem.CloneAppearance(target, user, targetAppearance, userAppearance);
    }
}

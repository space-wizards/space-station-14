using System.Linq;
using System.Threading;
using Content.Server.Administration.Commands;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Clothing.Components;
using Content.Server.Damage.Systems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Disease;
using Content.Shared.Electrocution;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tabletop.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly PolymorphableSystem _polymorphableSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly CreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GodmodeSystem _godmodeSystem = default!;

    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        Verb explode = new()
        {
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
            Act = () =>
            {
                var coords = Transform(args.Target).MapPosition;
                Timer.Spawn(_gameTiming.TickPeriod,
                    () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                        4, 1, 2, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
                    CancellationToken.None);

                if (TryComp(args.Target, out SharedBodyComponent? body))
                {
                    body.Gib();
                }
            },
            Impact = LogImpact.Extreme,
            Message = "Explode them.",
        };
        args.Verbs.Add(explode);

        // TODO: Port cluwne outfit.
        Verb clown = new()
        {
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Objects/Fun/bikehorn.rsi/icon.png",
            Act = () =>
            {
                SetOutfitCommand.SetOutfit(args.Target, "ClownGear", EntityManager, (target, clothing) =>
                {
                    if (HasComp<ClothingComponent>(clothing))
                        EnsureComp<UnremoveableComponent>(clothing);
                });
            },
            Impact = LogImpact.Extreme,
            Message = "Clowns them. The suit cannot be removed.",
        };
        args.Verbs.Add(clown);

        Verb chess = new()
        {
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Objects/Fun/Tabletop/chessboard.rsi/chessboard.png",
            Act = () =>
            {
                _godmodeSystem.EnableGodmode(args.Target); // So they don't suffocate.
                EnsureComp<TabletopDraggableComponent>(args.Target);
                RemComp<PhysicsComponent>(args.Target); // So they can be dragged around.
                var xform = Transform(args.Target);
                var board = Spawn("ChessBoard", xform.Coordinates);
                var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
                xform.Coordinates = EntityCoordinates.FromMap(_mapManager, session.Position);
            },
            Impact = LogImpact.Extreme,
            Message = "Chess dimension.",
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            Verb flames = new()
            {
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Interface/Alerts/Fire/fire.png",
                Act = () =>
                {
                    // Fuck you. Burn Forever.
                    flammable.FireStacks = 99999.9f;
                    _flammableSystem.Ignite(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = "Makes them burn.",
            };
            args.Verbs.Add(flames);
        }

        Verb monkey = new()
        {
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Mobs/Animals/monkey.rsi/dead.png",
            Act = () =>
            {
                _polymorphableSystem.PolymorphEntity(args.Target, "AdminMonkeySmite");
            },
            Impact = LogImpact.Extreme,
            Message = "Monkey mode.",
        };
        args.Verbs.Add(monkey);

        if (TryComp<DiseaseCarrierComponent>(args.Target, out var carrier))
        {
            Verb lungCancer = new()
            {
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Mobs/Species/Human/organs.rsi/lung-l.png",
                Act = () =>
                {
                    _diseaseSystem.TryInfect(carrier, _prototypeManager.Index<DiseasePrototype>("StageIIIALungCancer"));
                },
                Impact = LogImpact.Extreme,
                Message = "Stage IIIA Lung Cancer, for when they really like the hit show Breaking Bad.",
            };
            args.Verbs.Add(lungCancer);
        }

        if (TryComp<DamageableComponent>(args.Target, out var damageable) &&
            TryComp<MobStateComponent>(args.Target, out var mobState))
        {
            Verb hardElectrocute = new()
            {
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Clothing/Hands/Gloves/Color/yellow.rsi/icon.png",
                Act = () =>
                {
                    int damageToDeal = 0;
                    var critState = mobState._highestToLowestStates.Where(x => x.Value.IsCritical()).FirstOrNull();
                    if (critState is null)
                    {
                        // We can't crit them so try killing them.
                        var deadState = mobState._highestToLowestStates.Where(x => x.Value.IsDead()).FirstOrNull();
                        if (deadState is null)
                            return; // welp.

                        damageToDeal = deadState.Value.Key - (int) damageable.TotalDamage;
                    }
                    else
                    {
                        damageToDeal = critState.Value.Key - (int) damageable.TotalDamage;
                    }

                    if (damageToDeal <= 0)
                        damageToDeal = 100; // murder time.

                    if (_inventorySystem.TryGetSlots(args.Target, out var slotDefinitions))
                    {
                        foreach (var slot in slotDefinitions)
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var slotEnt))
                                continue;

                            RemComp<InsulatedComponent>(slotEnt.Value); // Fry the gloves.
                        }
                    }

                    _electrocutionSystem.TryDoElectrocution(args.Target, null, damageToDeal,
                        TimeSpan.FromSeconds(30), true);
                },
                Impact = LogImpact.Extreme,
                Message = "Electrocutes them, rendering anything they were wearing useless.",
            };
            args.Verbs.Add(hardElectrocute);
        }

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            Verb creamPie = new()
            {
                Category = VerbCategory.Smite,
                IconTexture = "/Textures/Objects/Consumable/Food/Baked/pie.rsi/plain-slice.png",
                Act = () =>
                {
                    _creamPieSystem.SetCreamPied(args.Target, creamPied, true);
                },
                Impact = LogImpact.Extreme,
                Message = "A cream pie, condensed into a button.",
            };
            args.Verbs.Add(creamPie);
        }
    }
}

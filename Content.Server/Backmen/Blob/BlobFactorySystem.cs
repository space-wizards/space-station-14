using Content.Server.Backmen.Blob.Components;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Backmen.Blob;

public sealed class BlobFactorySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobFactoryComponent, BlobTileGetPulseEvent>(OnPulsed);
        SubscribeLocalEvent<BlobFactoryComponent, ProduceBlobbernautEvent>(OnProduceBlobbernaut);
        SubscribeLocalEvent<BlobFactoryComponent, DestructionEventArgs>(OnDestruction);

    }

    private void OnDestruction(EntityUid uid, BlobFactoryComponent component, DestructionEventArgs args)
    {
        if (TryComp<BlobbernautComponent>(component.Blobbernaut, out var blobbernautComponent))
        {
            blobbernautComponent.Factory = null;
        }
    }

    private void OnProduceBlobbernaut(EntityUid uid, BlobFactoryComponent component, ProduceBlobbernautEvent args)
    {
        if (component.Blobbernaut != null)
            return;

        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null)
            return;

        if (!TryComp<BlobCoreComponent>(blobTileComponent.Core, out var blobCoreComponent))
            return;

        var xform = Transform(uid);

        var blobbernaut = Spawn(component.BlobbernautId, xform.Coordinates);

        component.Blobbernaut = blobbernaut;
        if (TryComp<BlobbernautComponent>(blobbernaut, out var blobbernautComponent))
        {
            blobbernautComponent.Factory = uid;
            blobbernautComponent.Color = blobCoreComponent.ChemСolors[blobCoreComponent.CurrentChem];
            Dirty(blobbernaut, blobbernautComponent);
        }
        if (TryComp<MeleeWeaponComponent>(blobbernaut, out var meleeWeaponComponent))
        {
            var blobbernautDamage = new DamageSpecifier();
            foreach (var keyValuePair in blobCoreComponent.ChemDamageDict[blobCoreComponent.CurrentChem].DamageDict)
            {
                blobbernautDamage.DamageDict.Add(keyValuePair.Key, keyValuePair.Value * 0.8f);
            }
            meleeWeaponComponent.Damage = blobbernautDamage;
        }
    }

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Phlogiston = "Phlogiston";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string TearGas = "TearGas";

    [ValidatePrototypeId<ReagentPrototype>]

    private const string Lexorin = "Lexorin";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Mold = "Mold";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Bicaridine = "Bicaridine";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Aluminium = "Aluminium";
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Iron = "Iron";
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Uranium = "Uranium";

    private void FillSmokeGas(Entity<BlobPodComponent> ent, BlobChemType currentChem)
    {
        var blobGas = EnsureComp<SmokeOnTriggerComponent>(ent).Solution;
        switch (currentChem)
        {
            case BlobChemType.BlazingOil:
                blobGas.AddSolution(new Solution(Phlogiston, FixedPoint2.New(30))
                {
                    Temperature = 1000
                },_prototypeManager);
                break;
            case BlobChemType.ReactiveSpines:
                blobGas.AddSolution(new Solution(Mold, FixedPoint2.New(30)),_prototypeManager);
                break;
            case BlobChemType.RegenerativeMateria:
                blobGas.AddSolution(new Solution(Bicaridine, FixedPoint2.New(30)),_prototypeManager);
                break;
            case BlobChemType.ExplosiveLattice:
                blobGas.AddSolution(new Solution(Lexorin, FixedPoint2.New(30))
                {
                    Temperature = 1000
                },_prototypeManager);
                break;
            case BlobChemType.ElectromagneticWeb:
                blobGas.AddSolution(new Solution(Aluminium, FixedPoint2.New(10)){ CanReact = false },_prototypeManager);
                blobGas.AddSolution(new Solution(Iron, FixedPoint2.New(10)){ CanReact = false },_prototypeManager);
                blobGas.AddSolution(new Solution(Uranium, FixedPoint2.New(10)){ CanReact = false },_prototypeManager);
                break;
            default:
                blobGas.AddSolution(new Solution(TearGas, FixedPoint2.New(30)),_prototypeManager);
                break;
        }
    }

    private void OnPulsed(EntityUid uid, BlobFactoryComponent component, BlobTileGetPulseEvent args)
    {
        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null)
            return;

        if (!TryComp<BlobCoreComponent>(blobTileComponent.Core, out var blobCoreComponent))
            return;

        if (component.SpawnedCount >= component.SpawnLimit)
            return;

        if (_gameTiming.CurTime < component.NextSpawn)
            return;

        var xform = Transform(uid);
        var pod = Spawn(component.Pod, xform.Coordinates);
        component.BlobPods.Add(pod);
        var blobPod = EnsureComp<BlobPodComponent>(pod);
        blobPod.Core = blobTileComponent.Core.Value;
        FillSmokeGas((pod,blobPod), blobCoreComponent.CurrentChem);

        //smokeOnTrigger.SmokeColor = blobCoreComponent.ChemСolors[blobCoreComponent.CurrentChem];
        component.SpawnedCount += 1;
        component.NextSpawn = _gameTiming.CurTime + TimeSpan.FromSeconds(component.SpawnRate);
    }

}

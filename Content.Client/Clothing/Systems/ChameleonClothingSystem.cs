using Content.Client.PDA;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emp;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Clothing.Systems;

// All valid items for chameleon are calculated on client startup and stored in dictionary.
public sealed class ChameleonClothingSystem : SharedChameleonClothingSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, AfterAutoHandleStateEvent>(HandleState);

        PrepareAllVariants();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReloaded);
    }

    private void OnProtoReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            PrepareAllVariants();
    }

    private void HandleState(EntityUid uid, ChameleonClothingComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(uid, component);
    }

    protected override void UpdateSprite(EntityUid uid, EntityPrototype proto)
    {
        base.UpdateSprite(uid, proto);

        if (TryComp(uid, out SpriteComponent? sprite)
            && proto.TryGetComponent(out SpriteComponent? otherSprite, Factory))
        {
            sprite.CopyFrom(otherSprite);
        }

        // Edgecase for PDAs to include visuals when UI is open
        if (TryComp(uid, out PdaBorderColorComponent? borderColor)
            && proto.TryGetComponent(out PdaBorderColorComponent? otherBorderColor, Factory))
        {
            borderColor.BorderColor = otherBorderColor.BorderColor;
            borderColor.AccentHColor = otherBorderColor.AccentHColor;
            borderColor.AccentVColor = otherBorderColor.AccentVColor;
        }
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Randomize EMP-affected clothing
        var query = EntityQueryEnumerator<EmpDisabledComponent, ChameleonClothingComponent>();
        while (query.MoveNext(out var uid, out _, out var chameleon))
        {
            if (!chameleon.EmpContinuous)
                continue;

            if (Timing.CurTime < chameleon.NextEmpChange)
                continue;

            // randomly pick cloth element from available and apply it
            var picked = GetRandomValidPrototype(chameleon.Slot, chameleon.RequireTag);
            chameleon.Default = picked;
            UpdateVisuals(uid, chameleon);

            chameleon.NextEmpChange = Timing.CurTime + TimeSpan.FromSeconds(1f / chameleon.EmpChangeIntensity);
        }
    }
}

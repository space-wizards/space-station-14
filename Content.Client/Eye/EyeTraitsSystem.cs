using Content.Client.Eye.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;
using EyeProtectionComponent = Content.Client.Eye.Blinding.EyeProtectionComponent;

namespace Content.Client.Eye;

public sealed class EyeTraitsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<EyeTraitsComponent, ComponentStartup>(OnEyeTraitStartup);
        SubscribeLocalEvent<EyeProtectionComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<EyeProtectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);
    }

    private void OnEyeTraitStartup(EntityUid uid, EyeTraitsComponent traits, ComponentStartup args)
    {
        if (!TryComp<EyeComponent>(uid, out var eye))
            return;

        UpdateEyes(traits, eye);
    }

    private void UpdateEyes(EyeTraitsComponent traits, EyeComponent eye)
    {
        // Apply in preference order mask, eyes, traits
        if (eye.Eye != null)
        {
            var newReduction = (traits.EyeProtection?.Reduction ?? 1.0f) * (traits.MaskProtection?.Reduction ?? 1.0f);
            var change = traits.Reduction / newReduction;

            // Sunglasses reduce the apparent brightness (EyeExposureSystem will turn it back up over time)
            eye.Eye.Exposure *= change;
            traits.Reduction = newReduction;

            // Sunglasses make you more tolerant to very bright light in the scene.
            eye.Eye.LightIntolerance = (traits.CurrentNight?.LightIntolerance ?? 0.5f) / newReduction;
        }
    }

    private void AddEyeProtection(EntityUid wearer, EntityUid glasses, EyeProtectionComponent protection,
        EyeTraitsComponent? traits = null, EyeComponent? eye = null)
    {
        if (!Resolve(wearer, ref traits) || !Resolve(wearer, ref eye))
            return;

        if (traits.EyeProtectionUid == glasses)
        {
            // Do nothing.
            return;
        }
        if (traits.EyeProtectionUid != EntityUid.Invalid)
        {
            // Ensure traits from old glasses gone first
            RemoveEyeProtection(wearer, traits.EyeProtectionUid, traits, eye);
        }

        traits.EyeProtectionUid = glasses;
        traits.EyeProtection = protection;

        UpdateEyes(traits, eye);
    }

    private void RemoveEyeProtection(EntityUid wearer, EntityUid glasses,
        EyeTraitsComponent? traits = null, EyeComponent? eye = null)
    {
        if (!Resolve(wearer, ref traits) || !Resolve(wearer, ref eye))
            return;

        // Not wearing these glasses. Ignore.
        if (traits.EyeProtectionUid != glasses)
            return;

        traits.EyeProtectionUid = EntityUid.Invalid;
        traits.EyeProtection = null;

        UpdateEyes(traits, eye);
    }


    private void AddMaskEyeProtection(EntityUid wearer, EntityUid mask, EyeProtectionComponent protection,
        EyeTraitsComponent? traits = null, EyeComponent? eye = null)
    {
        if (!Resolve(wearer, ref traits) || !Resolve(wearer, ref eye))
            return;

        if (traits.MaskProtectionUid == mask)
        {
            // Do nothing.
            return;
        }
        if (traits.MaskProtectionUid != EntityUid.Invalid)
        {
            // Ensure traits from old glasses gone first
            RemoveEyeProtection(wearer, traits.MaskProtectionUid, traits, eye);
        }

        traits.MaskProtectionUid = mask;
        traits.MaskProtection = protection;

        UpdateEyes(traits, eye);
    }

    private void RemoveMaskEyeProtection(EntityUid wearer, EntityUid mask,
        EyeTraitsComponent? traits = null, EyeComponent? eye = null)
    {
        if (!Resolve(wearer, ref traits) || !Resolve(wearer, ref eye))
            return;

        // Not wearing these glasses. Ignore.
        if (traits.MaskProtectionUid != mask)
            return;

        traits.MaskProtectionUid = EntityUid.Invalid;
        traits.MaskProtection = null;

        UpdateEyes(traits, eye);
    }

    private void OnGlassesEquipped(EntityUid uid, EyeProtectionComponent component, GotEquippedEvent args)
    {
        if ((args.SlotFlags & (SlotFlags.EYES)) != 0)
            AddEyeProtection(args.Equipee, args.Equipment, component);

        if ((args.SlotFlags & (SlotFlags.HEAD)) != 0)
            AddMaskEyeProtection(args.Equipee, args.Equipment, component);
    }

    private void OnGlassesUnequipped(EntityUid uid, EyeProtectionComponent component, GotUnequippedEvent args)
    {
        if ((args.SlotFlags & (SlotFlags.EYES)) != 0)
            RemoveEyeProtection(args.Equipee, args.Equipment);

        if ((args.SlotFlags & (SlotFlags.HEAD)) != 0)
            RemoveMaskEyeProtection(args.Equipee, args.Equipment);
    }

}

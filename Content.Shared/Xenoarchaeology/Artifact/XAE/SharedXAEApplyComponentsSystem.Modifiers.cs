using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

public partial class SharedXAEApplyComponentsSystem
{
    [Dependency] private HeldSpeedModifierSystem _heldSpeedModifier = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionsContainer = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;

    protected virtual bool TryApplyModifiers(
        IComponent component,
        XenoArtifactEffectsModifications modifications,
        EntityUid artifact
    )
    {
        return component switch
        {
            StorageComponent storage => TryApplyModifiersFor(storage, modifications),
            SolutionManagerComponent solutionContainer => TryApplyModifiersFor(solutionContainer, modifications, artifact),
            HeldSpeedModifierComponent speedModifier => TryApplyModifiersFor(speedModifier, modifications),
            MeleeWeaponComponent meleeWeapon => TryApplyModifiersFor(meleeWeapon, modifications),
            RevolverAmmoProviderComponent revolverAmmo => TryApplyModifiersFor(revolverAmmo, modifications),
            ToolComponent tool => TryApplyModifiersFor(tool, modifications),
            RadiationSourceComponent radiation => TryApplyModifiersFor(radiation, modifications, artifact),
            StealthOnMoveComponent stealthOnMove => TryApplyModifiersFor(stealthOnMove, modifications),
            _ => false
        };
    }

    private bool TryApplyModifiersFor(StealthOnMoveComponent stealthOnMove, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var effectivenessModifier))
        {
            stealthOnMove.PassiveVisibilityRate = effectivenessModifier.Modify(stealthOnMove.PassiveVisibilityRate);
            stealthOnMove.MovementVisibilityRate = effectivenessModifier.Modify(stealthOnMove.MovementVisibilityRate);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(
        RadiationSourceComponent radiationSource,
        XenoArtifactEffectsModifications modifications,
        EntityUid node
    )
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            _radiation.ChangeIntensity(node, modifier.Modify(radiationSource.Intensity));
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(ToolComponent tool, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            var newSpeedModifier = Math.Max(0.5f, modifier.Modify(tool.SpeedModifier));
            _tool.ChangeSpeedModifier(tool, newSpeedModifier);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(RevolverAmmoProviderComponent revolverAmmoProvider, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            revolverAmmoProvider.Capacity = Math.Max(1, (int) modifier.Modify(revolverAmmoProvider.Capacity));
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(MeleeWeaponComponent meleeWeapon, XenoArtifactEffectsModifications modifications)
    {
        var changed = false;
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var damageModifier))
        {
            foreach (var (key, value) in meleeWeapon.Damage.DamageDict)
            {
                meleeWeapon.Damage.DamageDict[key] = Math.Max(1, damageModifier.Modify(value.Value));
            }

            changed = true;
        }

        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            meleeWeapon.AttackRate = Math.Max(0.8f, modifier.Modify(meleeWeapon.AttackRate));
            changed = true;
        }

        return changed;
    }

    private bool TryApplyModifiersFor(HeldSpeedModifierComponent speedModifier, XenoArtifactEffectsModifications modifications)
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            _heldSpeedModifier.ChangeModifiers(
                speedModifier,
                modifier.Modify(speedModifier.SprintModifier),
                modifier.Modify(speedModifier.WalkModifier)
            );
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(
        SolutionManagerComponent solutionStorage,
        XenoArtifactEffectsModifications modifications,
        EntityUid artifact
    )
    {
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier)
            && _solutionsContainer.TryGetSolution((artifact, solutionStorage), "beaker", out var sol))
        {
            var modifiedMaxVolume = modifier.Modify(sol.Value.Comp.Solution.MaxVolume.Value);
            sol.Value.Comp.Solution.MaxVolume = MathF.Max(5, modifiedMaxVolume);
            return true;
        }

        return false;
    }

    private bool TryApplyModifiersFor(StorageComponent storage, XenoArtifactEffectsModifications modifications)
    {
        if (storage.Grid.Count <= 0)
            return false;

        var storageToModify = storage.Grid[0];
        var height = storageToModify.Height;
        var width = storageToModify.Width;
        var changed = false;
        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var heightModifier))
        {
            height = Math.Max(1, height + (int)heightModifier.Modify(height));
            changed = true;
        }

        if (modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var widthModifier))
        {
            width = Math.Max(1, width + (int)widthModifier.Modify(width));
            changed = true;
        }

        if(changed)
            storage.Grid[0] = new Box2i(0, 0, width, height);

        return changed;
    }
}

using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Database;
using Content.Shared.IgnitionSource;
using Content.Shared.Starlight.EntityEffects.Components;

namespace Content.Shared.Starlight.EntityEffects.EntitySystems;

public abstract class SharedDissolvableSystem : EntitySystem
{
    [Dependency] private readonly SharedIgnitionSourceSystem _ignitionSourceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    
    
    public void UpdateAppearance(EntityUid uid, DissolvableComponent? dissolvable = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref dissolvable, ref appearance))
            return;

        _appearance.SetData(uid, DissolveVisuals.OnDissolve, dissolvable.OnDissolve, appearance);
        _appearance.SetData(uid, DissolveVisuals.DissolveStacks, dissolvable.DissolveStacks, appearance);
    }

    public void AdjustDissolveStacks(EntityUid uid, float relativeFireStacks, DissolvableComponent? dissolvable = null, bool ignite = false)
    {
        if (!Resolve(uid, ref dissolvable))
            return;

        SetDissolveStacks(uid, dissolvable.DissolveStacks + relativeFireStacks, dissolvable, ignite);
    }

    public void SetDissolveStacks(EntityUid uid, float stacks, DissolvableComponent? dissolvable = null, bool ignite = false)
    {
        if (!Resolve(uid, ref dissolvable))
            return;

        dissolvable.DissolveStacks = MathF.Min(MathF.Max(dissolvable.MinimumDissolveStacks, stacks), dissolvable.MaximumDissolveStacks);

        if (dissolvable.DissolveStacks <= 0)
            Extinguish(uid, dissolvable);
        else
        {
            dissolvable.OnDissolve |= ignite;
            UpdateAppearance(uid, dissolvable);
        }
    }


    public void Extinguish(EntityUid uid, DissolvableComponent? dissolvable = null)
    {
        if (!Resolve(uid, ref dissolvable))
            return;

        if (!dissolvable.OnDissolve || !dissolvable.CanExtinguish)
            return;

        _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):entity} stopped being on dissolve damage");
        dissolvable.OnDissolve = false;
        dissolvable.DissolveStacks = 0;
        
        if (dissolvable.Effect != null)
        {
            EntityManager.QueueDeleteEntity(dissolvable.Effect);
            dissolvable.Effect = null;
        }

        _ignitionSourceSystem.SetIgnited(uid, false);

        var extinguished = new ExtinguishedEvent();
        RaiseLocalEvent(uid, ref extinguished);

        UpdateAppearance(uid, dissolvable);
    }
}
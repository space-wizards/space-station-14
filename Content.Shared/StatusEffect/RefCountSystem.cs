using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.StatusEffect;

/// <summary>
/// Manages adding and removing ref-counted components through a public API.
/// </summary>
public sealed class RefCountSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    private IComponentFactory _factory => EntityManager.ComponentFactory;

    #region Public API

    /// <summary>
    /// Add a ref-counted component by incrementing its sources list.
    /// You MUST call <see cref="Remove"/> afterwards or the component gets "leaked" and is never removed.
    /// You also MUST NOT remove the component through normal means or the reference count will be wrong.
    /// </summary>
    /// <returns>True if the component was just added</returns>
    public bool Add<T>(Entity<RefCountComponent?> ent) where T: IComponent, new()
    {
        return Add(ent, _factory.GetRegistration<T>().GetType());
    }

    /// <summary>
    /// Add a ref-counted from a reflected component type.
    /// Same rules apply from the generic version.
    /// </summary>
    public bool Add(Entity<RefCountComponent?> ent, Type type, bool force = false)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        ent.Comp ??= EnsureComp<RefCountComponent>(ent);
        var name = _factory.GetComponentName(type);

        if (HasComp(ent, type))
        {
            // increment it again if we already had the component and it wasn't refcounted before
            // this means when Add is counted on an entity that has the component already, calling remove after won't actually remove it
            // for prototypes this means they will never be removed by refcounting
            // for other systems they are in control of removing
            // example: reagent refcount-adds NoSlip to a mob, special mob that already has NoSlip drinks it. the reagent expiring shouldn't remove NoSlip.
            // once the reagent wears off it stays having a count of 1, ensuring it doesn't get removed
            if (GetCount(ent.Comp, name) == 0)
                Increment((ent, ent.Comp), name);
            if (!force)
            {
                Increment((ent, ent.Comp), name);
                return false;
            }
        }

        var comp = (Component) _factory.GetComponent(type);
        AddComp(ent, comp, force);
        return Increment((ent, ent.Comp), name);
    }

    /// <summary>
    /// Try to add all components from a <see cref="ComponentRegistry"/>.
    /// Same rules from <see cref="Add"/> apply, but you must call <see cref="RemoveComponents"/> instead.
    /// </summary>
    public void AddComponents(Entity<RefCountComponent?> ent, ComponentRegistry components, bool force = false)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp ??= EnsureComp<RefCountComponent>(ent);
        foreach (var reg in components.Values)
        {
            var type = reg.Component.GetType();
            var name = _factory.GetComponentName(type);
            if (Increment((ent, ent.Comp), name) && HasComp(ent, type))
                Increment((ent, ent.Comp), name);
        }
        EntityManager.AddComponents(ent, components, force);
    }

    /// <summary>
    /// Decrements the component's source count and removes it if there are no sources left.
    /// You MUST NOT call this without calling <see cref="Add"/> beforehand.
    /// </summary>
    /// <returns>Whether the component was actually removed</returns>
    public bool Remove<T>(Entity<RefCountComponent?> ent) where T: Component
    {
        return Remove(ent, typeof(T));
    }

    /// <summary>
    /// Decrements the component's source count and removes it if there are no sources left.
    /// This overload uses a type instead of generic type parameter.
    /// </summary>
    public bool Remove(Entity<RefCountComponent?> ent, Type type)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var name = _factory.GetComponentName(type);
        if (!Decrement((ent, ent.Comp), name))
            return false;

        RemComp(ent, type);
        return true;
    }

    /// <summary>
    /// Calls <see cref="Remove"/> for all components in a <see cref="ComponentRegistry"/>.
    /// Use this with <see cref="AddComponents"/> with the same components.
    /// </summary>
    public void RemoveComponents(Entity<RefCountComponent?> ent, ComponentRegistry components)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var reg in components.Values)
        {
            Remove((ent, ent.Comp), reg.Component.GetType());
        }
    }

    #endregion

    private uint GetCount(RefCountComponent comp, string name)
    {
        return comp.Counts.GetValueOrDefault(name);
    }

    private void SetCount(Entity<RefCountComponent> ent, string name, uint count)
    {
        if (count == 0)
            ent.Comp.Counts.Remove(name);
        else
            ent.Comp.Counts[name] = count;
        Dirty(ent);
    }

    private bool Increment(Entity<RefCountComponent> ent, string name)
    {
        var count = GetCount(ent.Comp, name) + 1;
        SetCount(ent, name, count);
        return count == 1;
    }

    private bool Decrement(Entity<RefCountComponent> ent, string name)
    {
        var count = GetCount(ent.Comp, name);
        if (_net.IsServer) // client isnt authoritative don't care
            DebugTools.Assert(count > 0, $"Tried to remove component {name} from {ToPrettyString(ent)} which was not added!");
        if (count == 0) // don't underflow
            return false;

        SetCount(ent, name, --count);
        return count == 0;
    }
}

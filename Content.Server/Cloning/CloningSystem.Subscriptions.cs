using System.Diagnostics.CodeAnalysis;
using Content.Server.Zombies;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Zombies;

namespace Content.Server.Cloning;

/// <summary>
/// The part of item cloning responsible for copying over important components.
/// </summary>
/// <remarks>
/// This is separate from the CloningSystem to place cloning logic closer together.
/// To exclude or add any specific copy code, place this in cloning context.
/// </remarks>
public sealed partial class CloningSystem
{
    [Dependency] private ZombieSystem _zombie = default!;

    #region Event Handlers
    // Please keep these alphabetized.
    [SubscribeLocalEvent]
    private void OnZombieCloned(Entity<ZombieComponent> ent, ref ClonedEvent args)
    {
        // Return the original's appearance to how it was before being zombified.
        _zombie.UnZombify(ent, args.CloneUid, ent.Comp);
    }
    #endregion Event Handlers

    /// <summary>
    /// Checks if a target entity has had the given component copied over to it, returning it into <paramref name="component"/>
    /// </summary>
    private bool Copied<T>(EntityUid target, CloningSettingsPrototype cloneSettings, [NotNullWhen(true)] out T? component) where T : Component
    {
        component = null;
        if (!cloneSettings.Components.Contains(Factory.GetRegistration(typeof(T)).Name))
            return false;

        return Resolve(target, ref component);
    }
}

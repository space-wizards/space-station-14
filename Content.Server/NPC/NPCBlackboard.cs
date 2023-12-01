using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Interaction;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.NPC;

[DataDefinition]
public sealed partial class NPCBlackboard : IEnumerable<KeyValuePair<string, object>>
{
    /// <summary>
    /// Global defaults for NPCs
    /// </summary>
    private static readonly Dictionary<string, object> BlackboardDefaults = new()
    {
        {"BufferRange", 10f},
        {"FollowCloseRange", 3f},
        {"FollowRange", 7f},
        {"IdleRange", 7f},
        {"InteractRange", SharedInteractionSystem.InteractionRange},
        {"MaximumIdleTime", 7f},
        {MedibotInjectRange, 4f},
        {MeleeMissChance, 0.3f},
        {"MeleeRange", 1f},
        {"MinimumIdleTime", 2f},
        {"MovementRangeClose", 0.2f},
        {"MovementRange", 1.5f},
        {"RangedRange", 10f},
        {"RotateSpeed", float.MaxValue},
        {"VisionRadius", 10f},
    };

    /// <summary>
    /// The specific blackboard for this NPC.
    /// </summary>
    private readonly Dictionary<string, object> _blackboard = new();

    /// <summary>
    /// Should we allow setting values on the blackboard. This is true when we are planning.
    /// <remarks>
    /// The effects get stored separately so they can potentially be re-applied during execution.
    /// </remarks>
    /// </summary>
    public bool ReadOnly = false;

    public void Clear()
    {
        _blackboard.Clear();
    }

    public NPCBlackboard ShallowClone()
    {
        var dict = new NPCBlackboard();
        foreach (var item in _blackboard)
        {
            dict.SetValue(item.Key, item.Value);
        }
        return dict;
    }

    [Pure]
    public bool ContainsKey(string key)
    {
        return _blackboard.ContainsKey(key);
    }

    /// <summary>
    /// Get the blackboard data for a particular key.
    /// </summary>
    [Pure]
    public T GetValue<T>(string key)
    {
        return (T) _blackboard[key];
    }

    /// <summary>
    /// Tries to get the blackboard data for a particular key. Returns default if not found
    /// </summary>
    [Pure]
    public T? GetValueOrDefault<T>(string key, IEntityManager entManager)
    {
        if (_blackboard.TryGetValue(key, out var value))
        {
            return (T) value;
        }

        if (TryGetEntityDefault(key, out value, entManager))
        {
            return (T) value;
        }

        if (BlackboardDefaults.TryGetValue(key, out value))
        {
            return (T) value;
        }

        return default;
    }

    /// <summary>
    /// Tries to get the blackboard data for a particular key.
    /// </summary>
    public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value, IEntityManager entManager)
    {
        if (_blackboard.TryGetValue(key, out var data))
        {
            value = (T) data;
            return true;
        }

        if (TryGetEntityDefault(key, out data, entManager))
        {
            value = (T) data;
            return true;
        }

        if (BlackboardDefaults.TryGetValue(key, out data))
        {
            value = (T) data;
            return true;
        }

        value = default;
        return false;
    }

    public void SetValue(string key, object value)
    {
        if (ReadOnly)
        {
            AssertReadonly();
            return;
        }

        _blackboard[key] = value;
    }

    private void AssertReadonly()
    {
        DebugTools.Assert(false, $"Tried to write to an NPC blackboard that is readonly!");
    }

    private bool TryGetEntityDefault(string key, [NotNullWhen(true)] out object? value, IEntityManager entManager)
    {
        value = default;
        EntityUid owner;

        switch (key)
        {
            case Access:
            {
                if (!TryGetValue(Owner, out owner, entManager))
                {
                    return false;
                }

                var access = entManager.EntitySysManager.GetEntitySystem<AccessReaderSystem>();
                value = access.FindAccessTags(owner);
                return true;
            }
            case ActiveHand:
            {
                if (!TryGetValue(Owner, out owner, entManager) ||
                    !entManager.TryGetComponent<HandsComponent>(owner, out var hands) ||
                    hands.ActiveHand == null)
                {
                    return false;
                }

                value = hands.ActiveHand;
                return true;
            }
            case ActiveHandFree:
            {
                if (!TryGetValue(Owner, out owner, entManager) ||
                    !entManager.TryGetComponent<HandsComponent>(owner, out var hands) ||
                    hands.ActiveHand == null)
                {
                    return false;
                }

                value = hands.ActiveHand.IsEmpty;
                return true;
            }
            case CanMove:
            {
                if (!TryGetValue(Owner, out owner, entManager))
                {
                    return false;
                }

                var blocker = entManager.EntitySysManager.GetEntitySystem<ActionBlockerSystem>();
                value = blocker.CanMove(owner);
                return true;
            }
            case FreeHands:
            {
                if (!TryGetValue(Owner, out owner, entManager) ||
                    !entManager.TryGetComponent<HandsComponent>(owner, out var hands) ||
                    hands.ActiveHand == null)
                {
                    return false;
                }

                var handos = new List<string>();

                foreach (var (id, hand) in hands.Hands)
                {
                    if (!hand.IsEmpty)
                        continue;

                    handos.Add(id);
                }

                value = handos;
                return true;
            }
            case Inventory:
            {
                if (!TryGetValue(Owner, out owner, entManager) ||
                    !entManager.TryGetComponent<HandsComponent>(owner, out var hands) ||
                    hands.ActiveHand == null)
                {
                    return false;
                }

                var handos = new List<string>();

                foreach (var (id, hand) in hands.Hands)
                {
                    if (!hand.IsEmpty)
                        continue;

                    handos.Add(id);
                }

                value = handos;
                return true;
            }
            case OwnerCoordinates:
            {
                if (!TryGetValue(Owner, out owner, entManager))
                {
                    return false;
                }

                if (entManager.TryGetComponent<TransformComponent>(owner, out var xform))
                {
                    value = xform.Coordinates;
                    return true;
                }

                return false;
            }
            default:
                return false;
        }
    }

    public bool Remove<T>(string key)
    {
        DebugTools.Assert(!_blackboard.ContainsKey(key) || _blackboard[key] is T);
        return _blackboard.Remove(key);
    }

    // I Ummd and Ahhd about using strings vs enums and decided on tags because
    // if a fork wants to do their own thing they don't need to touch the enum.

    /*
    * Constants to make development easier
    */

    public const string Access = "Access";
    public const string ActiveHand = "ActiveHand";
    public const string ActiveHandFree = "ActiveHandFree";
    public const string CanMove = "CanMove";
    public const string FreeHands = "FreeHands";
    public const string FollowTarget = "FollowTarget";
    public const string Inventory = "Inventory";
    public const string MedibotInjectRange = "MedibotInjectRange";

    public const string MeleeMissChance = "MeleeMissChance";

    public const string Owner = "Owner";
    public const string OwnerCoordinates = "OwnerCoordinates";
    public const string MovementTarget = "MovementTarget";

    /// <summary>
    /// Can the NPC click open entities such as doors.
    /// </summary>
    public const string NavInteract = "NavInteract";

    /// <summary>
    /// Can the NPC pry open doors for steering.
    /// </summary>
    public const string NavPry = "NavPry";

    /// <summary>
    /// Can the NPC smash obstacles for steering.
    /// </summary>
    public const string NavSmash = "NavSmash";

    /// <summary>
    /// Can the NPC climb obstacles for steering.
    /// </summary>
    public const string NavClimb = "NavClimb";

    /// <summary>
    /// Default key storage for a movement pathfind.
    /// </summary>
    public const string PathfindKey = "MovementPathfind";

    public const string RotateSpeed = "RotateSpeed";
    public const string VisionRadius = "VisionRadius";
    public const string UtilityTarget = "UtilityTarget";

    /// <summary>
    /// A configurable "order" enum that can be given to an NPC from an external source.
    /// </summary>
    public const string CurrentOrders = "CurrentOrders";

    /// <summary>
    /// A configurable target that's ordered by external sources.
    /// </summary>
    public const string CurrentOrderedTarget = "CurrentOrderedTarget";

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _blackboard.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

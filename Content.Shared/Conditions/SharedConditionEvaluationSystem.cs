using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Content.Shared.Conditions.HelperConditions;
using Content.Shared.Mindshield;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Shared.Conditions;

/// <summary>
/// The central API through which <see cref="IConditionByEvent" />s can be evaluated.
/// </summary>
public sealed partial class SharedConditionEvaluationSystem : EntitySystem
{
    /// <summary>
    /// added dependency to ensure all systems have been loaded.
    /// </summary>
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;

    [Dependency] private IReflectionManager _reflectionManager = default!;

    /// <summary>
    /// A lookup of ICondition types to a dedicated function that evaluates it.
    /// build using <see cref="CreateBindingsForEvaluator"/>
    /// </summary>
    private readonly Dictionary<Type, Func<ICondition, EntityUid, EntityUid?, float>> _bindings = [];

    public override void Initialize()
    {
        SetupBindings();
        //in case we load more assemblies during runtime, we need to create more bindings.
        _reflectionManager.OnAssemblyAdded += AssemblyLoaded;
        base.Initialize();
    }

    /// <summary>
    /// Rebuild bindings if assembly changed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AssemblyLoaded(object? sender, ReflectionUpdateEventArgs e)
    {
        _bindings.Clear();
        SetupBindings();
    }

    public override void Shutdown()
    {
        _reflectionManager.OnAssemblyAdded -= AssemblyLoaded;
        _bindings.Clear();
        base.Shutdown();
    }

    private void SetupBindings()
    {
        //step 1: create bindings for each evaluator system
        CreateBindingsForEvaluator();
        //step 2: reuse bindings for specific implementations.
        CreateConditionBindingsToEvaluatorBindings();
    }

    private void CreateConditionBindingsToEvaluatorBindings()
    {
        //cache all bindings, to avoid weird chains from interacting weirdly.
        List<KeyValuePair<Type, Func<ICondition, EntityUid, EntityUid?, float>>> bindingsToAdd = [];
        //go through all types
        foreach (var type in _reflectionManager.GetAllChildren(typeof(ICondition)))
        {
            //skip types that might be open.
            if (_bindings.ContainsKey(type))
                continue;
            //  Walk up the inheritance chain
            var currentBase = type;

            while (currentBase != null && currentBase != typeof(object))
            {
                //see if there is an existing binding.
                //fallback to non interface base conditions (should not be the case)
                var match = _bindings.Keys.FirstOrDefault(e => currentBase.GetInterfaces().Contains(e)) ??
                            _bindings.Keys.FirstOrDefault(e => currentBase.IsAssignableTo(e));
                if (match != null)
                {
                    bindingsToAdd.Add(new(type, _bindings[match]));
                    break;
                }
                // Move up to the next parent class
                currentBase = currentBase.BaseType;
            }
        }

        //register all bindings.
        foreach (var b in bindingsToAdd)
        {
            _bindings[b.Key] = b.Value;
        }
    }

    private void CreateBindingsForEvaluator()
    {
        // find all ConditionEvaluator Systems.

        //go through all types

        foreach (var type in _reflectionManager.GetAllChildren(typeof(EntitySystem)))
        {
            //skip types that might be open.
            if (type.IsAbstract || !type.IsSealed)
                continue;
            //  Walk up the inheritance chain (Because we might end up with a shared evaluator type in between step and server/client side evaluator at the bottom of the chain)
            var currentBase = type.BaseType;
            while (currentBase != null && currentBase != typeof(object))
            {
                //check if we reached the correct step
                if (currentBase.IsGenericType &&
                    currentBase.GetGenericTypeDefinition() == typeof(ConditionEvaluatorSystem<>))
                {
                    // Extract the type of the Condition we evaluate for.
                    var conditionType = currentBase.GetGenericArguments()[0];

                    RegisterConditionEvaluator(conditionType, type);

                    break; // Found it, no need to keep walking up the chain
                }

                // Move up to the next parent class
                currentBase = currentBase.BaseType;
            }
        }
    }

    /// <summary>
    /// Help function to create the entry in <see cref="_bindings"/> for a given condition and evaluator system pair.
    /// </summary>
    /// <param name="conditionType"></param>
    /// <param name="evaluatorSystemType"></param>
    private void RegisterConditionEvaluator(Type conditionType, Type evaluatorSystemType)
    {
        //define parameters
        var paramCondition = Expression.Parameter(typeof(ICondition), "condition");
        var paramEntity = Expression.Parameter(typeof(EntityUid), "entityUid");
        var paramSource = Expression.Parameter(typeof(EntityUid?), "sourceEntity");
        //add conversion of generic ICondition to Condition Type
        var cast = Expression.Convert(paramCondition, conditionType);
        //Resolve evaluator as a system.
        var evaluatorSystemObject = _entitySystemManager.GetEntitySystem(evaluatorSystemType);
        //feed object into expression
        var evaluatorConst = Expression.Constant(evaluatorSystemObject);
        //grab local method using the base type definition.
        var evaluateMethod = evaluatorSystemType.GetMethod(nameof(ConditionEvaluatorSystem<>.Evaluate));
        //assertion, which should never hit, because of how we got here in the first place.
        DebugTools.Assert(evaluateMethod != null,
            $"Somehow System {evaluatorSystemType.FullName} does not implement evaluation method!");
        //build the function System.Evaluate(Condition, Entity, Source)
        var call = Expression.Call(evaluatorConst, evaluateMethod, cast, paramEntity, paramSource);
        //add hint about typing of the expression
        var lambda =
            Expression.Lambda<Func<ICondition, EntityUid, EntityUid?, float>>(call,
                paramCondition,
                paramEntity,
                paramSource);
        //compile final lambda expression into IL and store in dictionary.
        if (!_bindings.TryAdd(conditionType, lambda.Compile()))
        {
            // we have a duplicate evaluator???
            throw new Exception("Duplicate evaluator for condition type: " + conditionType.FullName);
        }
    }

    /// <summary>
    /// Evaluates a condition against an entity given an optional source entity.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="entityUid">The entity on which the condition is to be tested.</param>
    /// <param name="sourceEntity"></param>
    /// <returns>
    /// A factor representing how much the condition is satisfied. For most binary conditions, this is 0 and 1, but
    /// anything gray it could be any arbitrary value
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// A condition that cannot be evaluated should not exist. Either you using it on
    /// an entity missing necessary components or there is no system to evaluate the condition
    /// </exception>
    public float EvaluateCondition(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (_bindings.TryGetValue(condition.GetType(), out var func))
        {
            return func(condition, entityUid, sourceEntity);
        }

        if (condition is not IConditionByEvent conditionByEvent)
        {
            throw new NotImplementedException("No Evaluation method for condition of type " +
                                              condition.GetType().FullName + " found!");
        }

        //make the event using our cached building function.
        var evt = conditionByEvent.WrapInEvent(entityUid, sourceEntity);
        if (evt == null)
            return 0f;
        // Use event to evaluate condition on entity.
        RaiseLocalEvent(entityUid, (object)evt);
        // Verity that the event was actually handled
        if (!evt.Handled)
        {
            //Add logging here?
        }

        //return response.
        return evt.Value;
    }

    /// <summary>
    /// Evaluates a condition against an entity given an optional source entity.
    /// Using additional interfaces on the condition to turn the result of the evaluation into a bool.
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="entityUid">The entity to check against</param>
    /// <param name="sourceEntity">An optional entity, likely the raiser of the evaluation</param>
    /// <returns></returns>
    /// <seealso cref="IWithInverted" />
    /// <seealso cref="IWithBoundary" />
    /// <seealso cref="IWithThreshold" />
    public bool IsConditionSatisfied(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return IsConditionSatisfied(condition, entityUid, out _, sourceEntity);
    }

    /// <summary>
    /// Evaluates a condition against an entity given an optional source entity.
    /// Using additional interfaces on the condition to turn the result of the evaluation into a bool.
    /// </summary>
    /// <param name="condition">The condition to check</param>
    /// <param name="entityUid">The entity to check against</param>
    /// <param name="scale"></param>
    /// <param name="sourceEntity">An optional entity, likely the raiser of the evaluation</param>
    /// <returns></returns>
    /// <remarks>
    /// Use this version if you want to use both the scale and satisfy result. Like EntityEffect check and then
    /// Scaling.
    /// </remarks>
    /// <seealso cref="IWithInverted" />
    /// <seealso cref="IWithBoundary" />
    /// <seealso cref="IWithThreshold" />
    public bool IsConditionSatisfied(ICondition condition,
        EntityUid entityUid,
        out float scale,
        EntityUid? sourceEntity = null)
    {
        scale = EvaluateCondition(condition, entityUid, sourceEntity);
        return EvalConditionWithInverted(condition, IsConditionSatisfied(condition, scale));
    }

    /// <summary>
    /// helper function to
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool IsConditionSatisfied(ICondition condition, float value)
    {
        //check scale now against the conditions limits.
        var satisfier = condition.Satisfier;
        if (satisfier == null &&
            condition is IConditionWithDefaultSatisfactionRule conditionWithDefaultSatisfactionRule)
            satisfier = conditionWithDefaultSatisfactionRule.GetDefaultSatisfier();
        if (satisfier == null)
            return value != 0;
        return satisfier.Inverted != satisfier.IsSatisfied(value);
    }

    /// <summary>
    /// Helper for Inverted Tag
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private bool EvalConditionWithInverted(ICondition condition, bool value)
    {
        if (condition is IWithInverted invertedCondition)
            return invertedCondition.Inverted != value;
        return value;
    }
}

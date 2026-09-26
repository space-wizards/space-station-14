using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;

[TypeSerializer]
public sealed class ModifierProviderBaseTypeSerializer : ITypeReader<ModifierProviderBase, ValueDataNode>
{
    private const string BudgetDependantAddCode = "budgetDependant add ";
    private const string BudgetDependantMultiplyCode = "budgetDependant multiply ";

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null
    )
    {
        // BudgetDependantAddModifierProvider validation
        if (node.Value.StartsWith(BudgetDependantAddCode) && ValidateArgumentsAreInt(node.Value[BudgetDependantAddCode.Length..]))
            return new ValidatedValueNode(node);

        if (node.Value.StartsWith(BudgetDependantMultiplyCode) && ValidateArgumentsAreFloat(node.Value[BudgetDependantMultiplyCode.Length..]))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    private bool ValidateArgumentsAreInt(string str)
    {
        var split = str.Split(',');
        if (split.Length != 2 && split.Length != 3)
            return false;

        foreach (var arg in split)
        {
            if (!int.TryParse(arg, out _))
                return false;
        }

        return true;
    }

    private bool ValidateArgumentsAreFloat(string str)
    {
        var split = str.Split(',');
        if (split.Length != 2 && split.Length != 3)
            return false;

        foreach (var arg in split)
        {
            if (!float.TryParse(arg, out _))
                return false;
        }

        return true;
    }

    public ModifierProviderBase Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ModifierProviderBase>? instanceProvider = null)
    {
        var type = typeof(ModifierProviderBase);
        if (node.Value.StartsWith(BudgetDependantAddCode)
            && TryParseBudgetDependantAddArgs(node.Value[BudgetDependantAddCode.Length..], out var addModifier))
            return addModifier;

        if (node.Value.StartsWith(BudgetDependantMultiplyCode)
            && TryParseBudgetDependantMultiplyArgs(node.Value[BudgetDependantAddCode.Length..], out var multiplyModifier))
            return multiplyModifier;
        
        return (ModifierProviderBase)serializationManager.Read(type, node, context)!;
    }

    private bool TryParseBudgetDependantAddArgs(string str, [NotNullWhen(true)] out BudgetDependantAddModifierProvider? modifierProvider)
    {
        var split = str.Split(',', StringSplitOptions.RemoveEmptyEntries);
        int min, max;
        if (split.Length == 2
            && int.TryParse(split[0], out min)
            && int.TryParse(split[1], out max))
        {
            modifierProvider = new BudgetDependantAddModifierProvider
            {
                Range = new Vector2(min, max),
            };
            return true;
        }

        if (split.Length == 3
            && int.TryParse(split[0], out min)
            && int.TryParse(split[1], out var center)
            && int.TryParse(split[2], out max)
        )
        {   
            modifierProvider = new BudgetDependantAddModifierProvider
            {
                Range = new Vector2(min, max),
                RangeCenter = center
            };
            return true;
        }

        modifierProvider = null;
        return false;
    }

    private bool TryParseBudgetDependantMultiplyArgs(
        string str,
        [NotNullWhen(true)] out BudgetDependantMultiplyModifierProvider? modifierProvider
    )
    {
        var split = str.Split(',', StringSplitOptions.RemoveEmptyEntries);
        int min, max;
        if (split.Length == 2
            && int.TryParse(split[0], out min)
            && int.TryParse(split[1], out max))
        {
            modifierProvider = new BudgetDependantMultiplyModifierProvider
            {
                Range = new Vector2(min, max),
            };
            return true;
        }

        if (split.Length == 3
            && int.TryParse(split[0], out min)
            && int.TryParse(split[1], out var center)
            && int.TryParse(split[2], out max)
           )
        {
            modifierProvider = new BudgetDependantMultiplyModifierProvider
            {
                Range = new Vector2(min, max),
                RangeCenter = center
            };
            return true;
        }

        modifierProvider = null;
        return false;
    }
}

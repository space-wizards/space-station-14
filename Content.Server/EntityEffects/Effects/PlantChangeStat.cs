using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantChangeStat : EntityEffect
{
    [DataField]
    public string TargetValue;

    [DataField]
    public float MinValue;

    [DataField]
    public float MaxValue;

    [DataField]
    public int Steps;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolder == null || plantHolder.Seed == null)
            return;

        var member = plantHolder.Seed.GetType().GetField(TargetValue);
        var mutationSys = args.EntityManager.System<MutationSystem>();

        if (member == null)
        {
            mutationSys.Log.Error(this.GetType().Name + " Error: Member " + TargetValue + " not found on " + plantHolder.GetType().Name + ". Did you misspell it?");
            return;
        }

        var currentValObj = member.GetValue(plantHolder.Seed);
        if (currentValObj == null)
            return;

        if (member.FieldType == typeof(float))
        {
            var floatVal = (float)currentValObj;
            mutationSys.MutateFloat(ref floatVal, MinValue, MaxValue, Steps);
            member.SetValue(plantHolder.Seed, floatVal);
        }
        else if (member.FieldType == typeof(int))
        {
            var intVal = (int)currentValObj;
            mutationSys.MutateInt(ref intVal, (int)MinValue, (int)MaxValue, Steps);
            member.SetValue(plantHolder.Seed, intVal);
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        throw new NotImplementedException();
    }
}

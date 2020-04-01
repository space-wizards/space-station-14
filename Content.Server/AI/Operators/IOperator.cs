namespace Content.Server.AI.HTN.Tasks.Primitive.Operators
{
    public interface IOperator
    {
        Outcome Execute(float frameTime);
    }

    public enum Outcome
    {
        Success,
        Continuing,
        Failed,
    }
}

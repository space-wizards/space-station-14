namespace Content.Server.AI.Operators
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

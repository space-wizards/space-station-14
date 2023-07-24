using System.Linq.Expressions;

namespace Content.Server.NewCon;

public sealed partial class NewConManager
{
    public bool IsTransformableTo(Type left, Type right)
    {
        throw new NotImplementedException();
    }

    public Expression GetTransformer(Type to, Type from, Expression input)
    {
        throw new NotImplementedException();
    }
}

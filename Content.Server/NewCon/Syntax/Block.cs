namespace Content.Server.NewCon.Syntax;

public sealed class Block
{
    public Expression Expression { get; set; }

    public Block(Expression expr)
    {
        Expression = expr;
    }
}


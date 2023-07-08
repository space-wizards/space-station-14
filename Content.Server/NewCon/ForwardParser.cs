using System.Text;

namespace Content.Server.NewCon;

public sealed class ForwardParser
{
    public readonly string Input;

    public int Index { get; private set; } = 0;

    public ForwardParser(string input)
    {
        Input = input;
    }

    public bool SpanInRange(int length)
    {
        return Input.Length > (Index + length - 1);
    }

    public char? PeekChar()
    {
        if (!SpanInRange(1))
            return null;

        return Input[Index];
    }

    public char? GetChar()
    {
        if (PeekChar() is { } c)
        {
            Index++;
            return c;
        }

        return null;
    }

    private string? MaybeGetWord(bool advanceIndex)
    {
        var startingIndex = Index;

        var builder = new StringBuilder();

        // Walk forward until we run into whitespace
        while (GetChar() is not ' ' and { } c) { builder.Append(c); }

        if (startingIndex == Index)
            return null;

        if (!advanceIndex)
            Index = startingIndex;

        return builder.ToString();
    }

    public string? PeekWord() => MaybeGetWord(false);

    public string? GetWord() => MaybeGetWord(true);

    public ParserRestorePoint Save()
    {
        return new ParserRestorePoint(Index);
    }

    public void Restore(ParserRestorePoint point)
    {
        Index = point.Index;
    }
}

public readonly record struct ParserRestorePoint(int Index);

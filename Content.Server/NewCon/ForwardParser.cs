using System.Runtime.CompilerServices;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Server.NewCon;

public sealed class ForwardParser
{
    [Robust.Shared.IoC.Dependency] public readonly NewConManager NewCon = default!;

    public readonly string Input;
    public int MaxIndex { get; private set; }

    public int Index { get; private set; } = 0;

    public ForwardParser(string input)
    {
        IoCManager.InjectDependencies(this);
        Input = input;
        MaxIndex = input.Length - 1;
    }

    private ForwardParser(ForwardParser parser, int sliceSize, int? index)
    {
        IoCManager.InjectDependencies(this);
        DebugTools.Assert(sliceSize > 0);
        Input = parser.Input;
        Index = index ?? parser.Index;
        MaxIndex = Math.Min(parser.MaxIndex, Index + sliceSize - 1);
    }

    public bool SpanInRange(int length)
    {
        return MaxIndex >= (Index + length - 1);
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

    private void DebugFun([CallerMemberName] string method = "")
    {
        Logger.DebugS("parser", $"{method}()");
    }

    public void DebugPrint()
    {
        Logger.DebugS("parser", Input);
        MakeDebugPointer(Index + 1);
        MakeDebugPointer(MaxIndex + 1, '|');
    }

    private void MakeDebugPointer(int pointAt, char pointer = '^')
    {
        var builder = new StringBuilder();
        builder.Append(' ', pointAt);
        builder.Append(pointer);
        Logger.DebugS("parser", builder.ToString());
    }

    private string? MaybeGetWord(bool advanceIndex, Func<char, bool>? test)
    {
        var startingIndex = Index;
        test ??= static c => c != ' ';

        var builder = new StringBuilder();

        Consume(char.IsWhiteSpace);

        // Walk forward until we run into whitespace
        while (PeekChar() is { } c && test(c)) { builder.Append(GetChar()); }

        if (startingIndex == Index)
            return null;

        if (!advanceIndex)
            Index = startingIndex;

        return builder.ToString();
    }

    public string? PeekWord(Func<char, bool>? test = null) => MaybeGetWord(false, test);

    public string? GetWord(Func<char, bool>? test = null) => MaybeGetWord(true, test);

    public ParserRestorePoint Save()
    {
        return new ParserRestorePoint(Index);
    }

    public void Restore(ParserRestorePoint point)
    {
        Index = point.Index;
    }

    public int Consume(Func<char, bool> control)
    {
        var amount = 0;

        while (PeekChar() is { } c && control(c))
        {
            GetChar();
            amount++;
        }

        return amount;
    }

    public ForwardParser? SliceBlock(char startDelim, char endDelim)
    {
        var checkpoint = Save();

        Consume(char.IsWhiteSpace);

        if (GetChar() != startDelim)
        {
            Restore(checkpoint);
            return null;
        }

        var blockStart = Index;

        var stack = 1;

        while (stack > 0)
        {
            var c = GetChar();
            if (c == startDelim)
                stack++;

            if (c == endDelim)
            {
                if (--stack == 0)
                    break;
            }

            if (c == null)
            {
                Restore(checkpoint);
                return null;
            }
        }

        return new ForwardParser(this, Index - blockStart, blockStart);
    }
}

public readonly record struct ParserRestorePoint(int Index);

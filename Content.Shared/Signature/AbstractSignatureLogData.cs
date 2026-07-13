namespace Content.Shared.Signature;

[Serializable]
public abstract class AbstractSignatureLogData(SignatureData data)
{
    public const string SignatureLogTag = "[Signature]";

    public override string ToString()
    {
        return $"{SignatureLogTag}({data.Width}x{data.Height})";
    }
}

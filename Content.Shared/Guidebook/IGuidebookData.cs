namespace Content.Shared.Guidebook;

public interface IGuidebookData
{
    IEnumerable<string> GetFieldNames();

    object GetFieldValue(string name);
}

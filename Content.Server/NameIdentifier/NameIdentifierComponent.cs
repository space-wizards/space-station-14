namespace Content.Server.NameIdentifier;

[RegisterComponent]
public class NameIdentifierComponent : Component
{
    [DataField("group", required: true)]
    public string Group = string.Empty;
}

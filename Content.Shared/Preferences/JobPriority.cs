
namespace Content.Shared.Preferences
{
    public enum JobPriority
    {
        // These enum values HAVE to match the ones in DbJobPriority in Content.Server.Database
        Never = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}

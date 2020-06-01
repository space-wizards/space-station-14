using System.Collections.Generic;

namespace Content.Server.Interfaces
{
    public interface IAccess
    {
        public List<string> GetTags();

        public void SetTags(List<string> newTags);
    }
}

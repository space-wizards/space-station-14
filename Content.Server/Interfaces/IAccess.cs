using System.Collections.Generic;

namespace Content.Server.Interfaces
{
    //Maybe make like an IAccessComponent, have the PDA implement it, and just pass through the access tags on the ID?
    public interface IAccess
    {
        public List<string> GetTags();

        public void SetTags(List<string> newTags);
    }
}

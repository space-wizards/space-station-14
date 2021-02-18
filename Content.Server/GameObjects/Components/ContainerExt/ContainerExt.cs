using Robust.Server.GameObjects;

namespace Content.Server.GameObjects.Components.ContainerExt
{
    public static class ContainerExt
    {
        public static int CountPrototypeOccurencesRecursive(this ContainerManagerComponent mgr, string prototypeId)
        {
            int total = 0;
            foreach (var container in mgr.GetAllContainers())
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (entity.Prototype?.ID == prototypeId) total++;
                    if(!entity.TryGetComponent<ContainerManagerComponent>(out var component)) continue;
                    total += component.CountPrototypeOccurencesRecursive(prototypeId);
                }
            }

            return total;
        }
    }
}

using System.Collections.Generic;

namespace ECS
{
    public abstract class BaseSystem
    {
        public abstract void Execute(IEnumerable<ECSEntity> entities);
    }
}
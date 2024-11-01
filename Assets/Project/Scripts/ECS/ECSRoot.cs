using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace ECS
{
    public class ECSRoot
    {
        private List<ECSEntity> entities;
        private List<BaseSystem> systems;

        public void Initialize()
        {
            entities = new();
            systems = new();

            var subs = FindAllSubClasses<BaseSystem>();
            foreach (var subclass in subs)
                systems.Add(Activator.CreateInstance(subclass) as BaseSystem);
        }

        public List<ECSEntity> GetAllEntities() => entities;

        public void Execute<T>() where T : BaseSystem
        {
            var system = systems.FirstOrDefault(s => s is T);

            if (system == null) return;

            system.Execute(entities);
        }

        public void AddEntity(ECSEntity entity)
        {
            if(entities.Contains(entity))
                throw new Exception(entity.GetType().FullName);

            entities.Add(entity);
        }
        public void RemoveEntity(ECSEntity entity)
        {
            if (entities.Contains(entity) == false)
                throw new Exception(entity.GetType().FullName);

            entities.Remove(entity);
        }

        private static Type[] FindAllSubClasses<T>()
        {
            Type baseType = typeof(T);
            Assembly assembly = Assembly.GetAssembly(baseType);

            Type[] types = assembly.GetTypes();
            Type[] subclasses = types.Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract).ToArray();

            return subclasses;
        }
    }
}
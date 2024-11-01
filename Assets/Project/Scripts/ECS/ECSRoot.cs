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

        public void ExecuteAll<T>() where T : BaseSystem
        {
            var targetSystems = systems.FindAll(s => s is T);

            foreach (var targetSystem in targetSystems)
                foreach (var entity in entities)
                    targetSystem.Execute(entity);
        }

        public void DefineNewEntity(ECSEntity entity)
        {
            if(entities.Contains(entity))
                throw new Exception(entity.GetType().FullName);

            entities.Add(entity);
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
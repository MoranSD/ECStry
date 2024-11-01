using System.Collections.Generic;
using System;

namespace ECS
{
    public class ECSEntity
    {
        private List<EntityComponent> components = new List<EntityComponent>();

        public T Define<T>() where T : EntityComponent, new()
        {
            var component = Get<T>();

            if (component != null)
                return component;

            var newComponent = new T();
            components.Add(newComponent);
            return newComponent;
        }
        public bool Is<T>(out T component) where T : EntityComponent, new()
        {
            component = Get<T>();
            return component != null;
        }
        public bool Is<T>() where T : EntityComponent, new()
        {
            return Get<T>() != null;
        }
        public bool Is(Type type)
        {
            return components.Find(c => c.GetType() == type) != null;
        }
        public T Get<T>() where T : EntityComponent, new()
        {
            return components.Find(c => c is T) as T;
        }
    }
}
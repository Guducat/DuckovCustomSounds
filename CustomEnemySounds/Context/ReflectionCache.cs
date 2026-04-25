using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace DuckovCustomSounds.CustomEnemySounds.Context
{
    /// <summary>
    /// Caches reflection lookups for frequently accessed members.
    /// </summary>
    internal static class ReflectionCache
    {
        private sealed class PropertyAccessor : MemberAccessor
        {
            private readonly PropertyInfo _property;

            public PropertyAccessor(PropertyInfo property) => _property = property;

            public override object GetValue(object target) => _property.GetValue(target);
        }

        private sealed class FieldAccessor : MemberAccessor
        {
            private readonly FieldInfo _field;

            public FieldAccessor(FieldInfo field) => _field = field;

            public override object GetValue(object target) => _field.GetValue(target);
        }

        private abstract class MemberAccessor
        {
            public abstract object GetValue(object target);
        }

        private static readonly ConcurrentDictionary<(Type, string), MemberAccessor> Cache =
            new ConcurrentDictionary<(Type, string), MemberAccessor>();

        public static object GetValue(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;

            var type = target.GetType();
            var key = (type, name);

            if (Cache.TryGetValue(key, out var accessor))
            {
                try { return accessor.GetValue(target); }
                catch { return null; }
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            try
            {
                var property = type.GetProperty(name, flags);
                if (property != null)
                {
                    accessor = new PropertyAccessor(property);
                    Cache[key] = accessor;
                    return accessor.GetValue(target);
                }
            }
            catch
            {
            }

            try
            {
                var field = type.GetField(name, flags);
                if (field != null)
                {
                    accessor = new FieldAccessor(field);
                    Cache[key] = accessor;
                    return accessor.GetValue(target);
                }
            }
            catch
            {
            }

            Cache[key] = MissingAccessor.Instance;
            return null;
        }

        public static void Clear() => Cache.Clear();

        private sealed class MissingAccessor : MemberAccessor
        {
            public static readonly MissingAccessor Instance = new MissingAccessor();

            private MissingAccessor()
            {
            }

            public override object GetValue(object target) => null;
        }
    }
}

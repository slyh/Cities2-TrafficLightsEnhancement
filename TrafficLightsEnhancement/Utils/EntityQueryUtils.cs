using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Utils
{
    public class EntityQueryUtils
    {
        public static EntityQuery GetEntityQuery(object obj, string fieldName)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (EntityQuery)fieldInfo.GetValue(obj);
        }

        public static void SetEntityQuery(object obj, string fieldName, EntityQuery entityQuery)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(obj, entityQuery);
        }

        public static void UpdateEntityQuery(SystemBase systemBase, string fieldName, NativeList<ComponentType> none)
        {
            EntityQuery query = GetEntityQuery(systemBase, fieldName);
            EntityQuery newQuery = GetEntityQueryBuilder(query, none).Build(systemBase);
            SetEntityQuery(systemBase, fieldName, newQuery);
        }

        public static EntityQueryBuilder GetEntityQueryBuilder(EntityQuery oldQuery, NativeList<ComponentType> none)
        {
            var empty = new NativeList<ComponentType>(0, Allocator.Temp);
            return GetEntityQueryBuilder(oldQuery, empty, none, empty, empty, empty, empty);
        }

        public static EntityQueryBuilder GetEntityQueryBuilder
        (
            EntityQuery oldQuery,
            NativeList<ComponentType> any,
            NativeList<ComponentType> none,
            NativeList<ComponentType> all,
            NativeList<ComponentType> disabled,
            NativeList<ComponentType> absent,
            NativeList<ComponentType> present
        )
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);
            var descArray = oldQuery.GetEntityQueryDescs();
            for (int i = 0; i < descArray.Length; i++)
            {
                EntityQueryDesc desc = descArray[i];
                var oldAny = CreateNativeList(desc.Any, Allocator.Temp);
                var oldNone = CreateNativeList(desc.None, Allocator.Temp);
                var oldAll = CreateNativeList(desc.All, Allocator.Temp);
                var oldDisabled = CreateNativeList(desc.Disabled, Allocator.Temp);
                var oldAbsent = CreateNativeList(desc.Absent, Allocator.Temp);
                var oldPresent = CreateNativeList(desc.Present, Allocator.Temp);
                builder.WithAny(ref oldAny);
                builder.WithNone(ref oldNone);
                builder.WithAll(ref oldAll);
                builder.WithDisabled(ref oldDisabled);
                builder.WithAbsent(ref oldAbsent);
                builder.WithPresent(ref oldPresent);
                builder.WithAny(ref any);
                builder.WithNone(ref none);
                builder.WithAll(ref all);
                builder.WithDisabled(ref disabled);
                builder.WithAbsent(ref absent);
                builder.WithPresent(ref present);
                if (i < descArray.Length - 1)
                {
                    builder.AddAdditionalQuery();
                }
            }
            return builder;
        }

        public static NativeList<T> CreateNativeList<T>(T[] array, Allocator allocator) where T : unmanaged
        {
            var list = new NativeList<T>(array.Length, allocator);
            foreach (var item in array)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
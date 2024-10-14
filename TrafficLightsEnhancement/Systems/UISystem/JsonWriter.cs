using System.Collections.Generic;
using C2VM.TrafficLightsEnhancement.Utils;
using Colossal.UI.Binding;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem.JsonWriter;

public class EdgeInfoWriter : IWriter<Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>>>
{
    public void Write(IJsonWriter writer, Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> value)
    {
        int totalLength = 0;
        var dictionaryValues = value.Values;
        foreach (var edgeInfoArray in dictionaryValues)
        {
            totalLength += edgeInfoArray.Length;
        }
        writer.ArrayBegin(totalLength);
        foreach (var edgeInfoArray in dictionaryValues)
        {
            foreach (var edgeInfo in edgeInfoArray)
            {
                writer.Write(edgeInfo);
            }
        }
        writer.ArrayEnd();
    }
}

public class FalseEqualityComparer<T> : EqualityComparer<T>
{
    public override bool Equals(T x, T y)
    {
        return false;
    }

    public override int GetHashCode(T obj)
    {
        return 0;
    }
}
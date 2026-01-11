using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZUI.Utils;

public static class UnityHelper
{
    public static GameObject FindInHierarchy(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        string[] segments = path.Split('|');
        if (segments.Length == 0)
            return null;

        // Start with the root object
        GameObject current = GameObject.Find(segments[0]);
        if (current == null)
            return null;

        // Navigate through the hierarchy path
        for (int i = 1; i < segments.Length; i++)
        {
            Transform child = current.transform.Find(segments[i]);
            if (child == null)
                return null;

            current = child.gameObject;
        }

        return current;
    }

    public static void LogShader(Material material)
    {
        var cnt = material.shader.GetPropertyCount();
        for (int i = 0; i < cnt; i++)
        {
            var name = material.shader.GetPropertyName(i);
            var desc = material.shader.GetPropertyDescription(i);
            var attr = material.shader.GetPropertyAttributes(i);
            var flags = material.shader.GetPropertyFlags(i);
            var type = material.shader.GetPropertyType(i);
            object def = null; //
            var range = type == ShaderPropertyType.Range
                ? material.shader.GetPropertyRangeLimits(i)
                : Vector2.zero;
            switch (type)
            {
                case ShaderPropertyType.Vector:
                    def = material.shader.GetPropertyDefaultVectorValue(i);
                    break;
                case ShaderPropertyType.Float:
                    def = material.shader.GetPropertyDefaultFloatValue(i);
                    break;
                case ShaderPropertyType.Int:
                    def = material.shader.GetPropertyDefaultIntValue(i);
                    break;
            }

            LogUtils.LogInfo(
                $"Property {i}: {name} - {desc} - {(attr != null ? string.Join(',', attr.Select(a => a)) : null)} - {flags} - {type} - {def} - {range.x}:{range.y}");
        }
    }
}

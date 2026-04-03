using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EventTypeReference))]
public class EventTypeReferenceDrawer : PropertyDrawer
{
    static List<Type> _cachedEventTypes;
    static string[] _cachedDisplayNames;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty typeNameProp = property.FindPropertyRelative("assemblyQualifiedName");
        if (typeNameProp == null)
        {
            EditorGUI.LabelField(position, label.text, "Invalid EventTypeReference");
            return;
        }

        EnsureCache();

        int selectedIndex = 0;
        string currentTypeName = typeNameProp.stringValue;
        if (!string.IsNullOrWhiteSpace(currentTypeName))
        {
            int foundIndex = _cachedEventTypes.FindIndex(t => string.Equals(t.AssemblyQualifiedName, currentTypeName, StringComparison.Ordinal));
            if (foundIndex >= 0)
                selectedIndex = foundIndex + 1;
        }

        EditorGUI.BeginProperty(position, label, property);
        int nextIndex = EditorGUI.Popup(position, label.text, selectedIndex, _cachedDisplayNames);
        if (nextIndex != selectedIndex)
        {
            typeNameProp.stringValue = nextIndex == 0
                ? string.Empty
                : _cachedEventTypes[nextIndex - 1].AssemblyQualifiedName;
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    static void EnsureCache()
    {
        if (_cachedEventTypes != null && _cachedDisplayNames != null)
            return;

        var types = new List<Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);

        foreach (Assembly assembly in assemblies)
        {
            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                assemblyTypes = ex.Types.Where(t => t != null).ToArray();
            }

            for (int i = 0; i < assemblyTypes.Length; i++)
            {
                Type type = assemblyTypes[i];
                if (type == null) continue;
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters) continue;
                if (!typeof(IEvent).IsAssignableFrom(type)) continue;
                types.Add(type);
            }
        }

        _cachedEventTypes = types
            .Distinct()
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var displayNames = new List<string> { "None" };
        for (int i = 0; i < _cachedEventTypes.Count; i++)
            displayNames.Add(_cachedEventTypes[i].Name);

        _cachedDisplayNames = displayNames.ToArray();
    }
}

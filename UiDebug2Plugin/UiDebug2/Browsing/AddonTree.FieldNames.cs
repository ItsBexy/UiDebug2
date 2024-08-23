using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

using static System.Reflection.BindingFlags;
using static UiDebug2.UiDebug2;

namespace UiDebug2.Browsing;

public unsafe partial class AddonTree
{
    private static readonly Dictionary<string, Type?> AddonTypeDict = new();

    internal Dictionary<nint, List<string>> FieldNames { get; set; } = new();

    internal static object? GetAddonObj(string addonName, AtkUnitBase* addon)
    {
        if (addon == null)
        {
            return null;
        }

        if (!AddonTypeDict.ContainsKey(addonName))
        {
            AddonTypeDict.Add(addonName, null);

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in from t in a.GetTypes()
                                      where t.IsPublic
                                      let xivAddonAttr = (Addon?)t.GetCustomAttribute(typeof(Addon), false)
                                      where xivAddonAttr != null
                                      where xivAddonAttr.AddonIdentifiers.Contains(addonName)
                                      select t)
                    {
                        AddonTypeDict[addonName] = t;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{ex}");
                }
            }
        }

        return AddonTypeDict.TryGetValue(addonName, out var result) && result != null ? Marshal.PtrToStructure(new(addon), result) : *addon;
    }

    private void PopulateFieldNames(nint ptr)
    {
        var addonObj = GetAddonObj(this.AddonName, (AtkUnitBase*)ptr);
        this.PopulateFieldNames(addonObj, ptr);
    }

    private void PopulateFieldNames(object? obj, nint baseAddr, List<string>? path = null)
    {
        if (obj == null)
        {
            return;
        }

        path ??= new List<string>();
        var baseType = obj.GetType();

        foreach (var field in baseType.GetFields(Static | Public | NonPublic | Instance))
        {
            if (field.GetCustomAttribute(typeof(FieldOffsetAttribute)) is FieldOffsetAttribute offset)
            {
                var fieldAddr = baseAddr + offset.Value;
                var name = field.Name[0] == '_' ? char.ToUpper(field.Name[1]) + field.Name[2..] : field.Name;
                var fieldType = field.FieldType;

                if (!field.IsStatic && fieldType.IsPointer)
                {
                    var pointer = (nint)Pointer.Unbox((Pointer)field.GetValue(obj)!);
                    var itemType = fieldType.GetElementType();
                    ParsePointer(fieldAddr, pointer, itemType, name);
                }
                else if (fieldType.IsExplicitLayout)
                {
                    ParseExplicitField(fieldAddr, field, fieldType, name);
                }
                else if (fieldType.Name.Contains("FixedSizeArray"))
                {
                    ParseFixedSizeArray(fieldAddr, fieldType, name);
                }
            }
        }

        void ParseExplicitField(nint fieldAddr, FieldInfo field, MemberInfo fieldType, string name)
        {
            if (this.FieldNames.TryAdd(fieldAddr, new List<string>(path) { name }) && fieldType.DeclaringType == baseType)
            {
                this.PopulateFieldNames(field.GetValue(obj), fieldAddr, new List<string>(path) { name });
            }
        }

        void ParseFixedSizeArray(nint fieldAddr, Type fieldType, string name)
        {
            var spanLength = (int)(fieldType.CustomAttributes.ToArray()[0].ConstructorArguments[0].Value ?? 0);

            if (spanLength <= 0)
            {
                return;
            }

            var itemType = fieldType.UnderlyingSystemType.GenericTypeArguments[0];

            if (!itemType.IsGenericType)
            {
                var size = Marshal.SizeOf(itemType);
                for (var i = 0; i < spanLength; i++)
                {
                    var itemAddr = fieldAddr + (size * i);
                    var itemName = $"{name}[{i}]";

                    this.FieldNames.TryAdd(itemAddr, new List<string>(path) { itemName });

                    var item = Marshal.PtrToStructure(itemAddr, itemType);
                    if (itemType.DeclaringType == baseType)
                    {
                        this.PopulateFieldNames(item, itemAddr, new List<string>(path) { name });
                    }
                }
            }
            else if (itemType.Name.Contains("Pointer"))
            {
                itemType = itemType.GenericTypeArguments[0];

                for (var i = 0; i < spanLength; i++)
                {
                    var itemAddr = fieldAddr + (0x08 * i);
                    var pointer = Marshal.ReadIntPtr(itemAddr);
                    ParsePointer(itemAddr, pointer, itemType, $"{name}[{i}]");
                }
            }
        }

        void ParsePointer(nint fieldAddr, nint pointer, Type? itemType, string name)
        {
            if (pointer == 0)
            {
                return;
            }

            this.FieldNames.TryAdd(fieldAddr, new List<string>(path) { name });
            this.FieldNames.TryAdd(pointer, new List<string>(path) { name });

            if (itemType?.DeclaringType != baseType || itemType.IsPointer)
            {
                return;
            }

            var item = Marshal.PtrToStructure(pointer, itemType);
            this.PopulateFieldNames(item, pointer, new List<string>(path) { name });
        }
    }
}

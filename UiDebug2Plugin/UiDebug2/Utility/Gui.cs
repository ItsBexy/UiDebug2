using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static ImGuiNET.ImGuiCol;

namespace UiDebug2.Utility;

internal static class Gui
{
    internal static bool NestedTreePush(string label, out Vector2 lineStart, bool defOpen = false)
    {
        var imGuiTreeNodeFlags = ImGuiTreeNodeFlags.SpanFullWidth;

        if (defOpen)
        {
            imGuiTreeNodeFlags |= ImGuiTreeNodeFlags.DefaultOpen;
        }

        var treeNodeEx = ImGui.TreeNodeEx(label, imGuiTreeNodeFlags);
        lineStart = ImGui.GetCursorScreenPos() + new Vector2(-10, 2);
        return treeNodeEx;
    }

    internal static bool NestedTreePush(string label, Vector4 color, out Vector2 lineStart, bool defOpen = false)
    {
        ImGui.PushStyleColor(Text, color);
        var result = NestedTreePush(label, out lineStart, defOpen);
        ImGui.PopStyleColor();
        return result;
    }

    internal static void NestedTreePop(Vector2 lineStart, Vector4? color = null)
    {
        var lineEnd = lineStart with { Y = ImGui.GetCursorScreenPos().Y - 7 };

        if (lineStart.Y < lineEnd.Y)
        {
            ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, RgbaVector4ToUint(color ?? new(1)), 1);
        }

        ImGui.TreePop();
    }

    internal static unsafe bool IconSelectInput<T>(string label, ref T val, List<T> options, List<FontAwesomeIcon> icons)
    {
        var ret = false;
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var icon = icons[i];

            if (i > 0)
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ((ImGui.GetFontSize() / -6f) + 7f));
            }

            var color = *ImGui.GetStyleColorVec4(val is not null && val.Equals(option) ? ButtonActive : Button);

            if (ImGuiComponents.IconButton($"{label}{option}{i}", icon, color))
            {
                val = option;
                ret = true;
            }
        }

        return ret;
    }

    internal static void PrintFieldValuePairs(params (string FieldName, string Value)[] pairs)
    {
        for (var i = 0; i < pairs.Length; i++)
        {
            if (i != 0)
            {
                ImGui.SameLine();
            }

            PrintFieldValuePair(pairs[i].FieldName, pairs[i].Value, false);
        }
    }

    internal static void PrintFieldValuePair(string label, string copyText, bool copy = true)
    {
        ImGui.Text($"{label}:");
        ImGui.SameLine();
        if (copy)
        {
            ClickToCopyText(copyText);
        }
        else
        {
            ImGui.TextColored(new(0.6f, 0.6f, 0.6f, 1), copyText);
        }
    }

    internal static void PrintColor(ByteColor color, string fmt) => PrintColor(RgbaUintToVector4(color.RGBA), fmt);

    internal static void PrintColor(Vector3 color, string fmt) => PrintColor(new Vector4(color, 1), fmt);

    internal static void PrintColor(Vector4 color, string fmt)
    {
        static double Luminosity(Vector4 vector4) =>
            Math.Pow(
                (Math.Pow(vector4.X, 2) * 0.299f) +
                (Math.Pow(vector4.Y, 2) * 0.587f) +
                (Math.Pow(vector4.Z, 2) * 0.114f),
                0.5f) * vector4.W;

        ImGui.PushStyleColor(Text, Luminosity(color) < 0.5f ? new Vector4(1) : new(0, 0, 0, 1));
        ImGui.PushStyleColor(Button, color);
        ImGui.PushStyleColor(ButtonActive, color);
        ImGui.PushStyleColor(ButtonHovered, color);
        ImGui.SmallButton(fmt);
        ImGui.PopStyleColor(4);
    }

    internal static void ClickToCopyText(string text, string? textCopy = null)
    {
        ImGui.PushStyleColor(Text, new Vector4(0.6f, 0.6f, 0.6f, 1));
        ImGuiHelpers.ClickToCopyText(text, textCopy);
        ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{textCopy ?? text}");
        }
    }

    internal static bool SplitTooltip(params string[] tooltips)
    {
        if (!ImGui.IsItemHovered())
        {
            return false;
        }

        var mouseX = ImGui.GetMousePos().X;
        var minX = ImGui.GetItemRectMin().X;
        var maxX = ImGui.GetItemRectMax().X;
        var prog = (mouseX - minX) / (maxX - minX);

        var index = (int)Math.Floor(prog * tooltips.Length);

        ImGui.SetTooltip(tooltips[index]);

        return true;
    }
}

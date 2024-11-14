using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;
using ImGuiScene;

namespace UiDebug2.Utility;

/// <summary>
/// Class containing various helper methods for use with ImGui inside Dalamud.
/// </summary>
public static partial class ImGuiHelpers
{
  
    /// <summary>
    /// Print out text that can be copied when clicked.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="textCopy">The text to copy when clicked.</param>
    /// <param name="color">The color of the text.</param>
    public static void ClickToCopyText(string text, string? textCopy = null, Vector4? color = null)
    {
        textCopy ??= text;

        using (var col = new ImRaii.Color())
        {
            if (color.HasValue)
            {
                col.Push(ImGuiCol.Text, color.Value);
            }

            ImGui.TextUnformatted($"{text}");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            using (ImRaii.Tooltip())
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    ImGui.TextUnformatted(FontAwesomeIcon.Copy.ToIconString());
                }

                ImGui.SameLine();
                ImGui.TextUnformatted(textCopy);
            }
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(textCopy);
        }
    }



}

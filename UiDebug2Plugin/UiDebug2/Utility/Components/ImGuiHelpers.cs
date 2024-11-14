using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace UiDebug2.Utility;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Class containing various helper methods for use with ImGui inside Dalamud.
/// </summary>
public static class ImGuiHelpers
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

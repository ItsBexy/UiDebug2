using System.Diagnostics.CodeAnalysis;

using ImGuiNET;

namespace Dalamud.Interface.Components;

/// <summary>
/// Class containing various methods providing ImGui components.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static partial class ImGuiComponents
{
    /// <summary>
    /// Test component to demonstrate how ImGui components work.
    /// </summary>
    public static void Test()
    {
        ImGui.Text("You are viewing the test component. The test was a success.");
    }
}

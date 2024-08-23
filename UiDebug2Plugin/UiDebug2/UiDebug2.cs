using System;
using System.Collections.Generic;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using UiDebug2.Browsing;

using static ImGuiNET.ImGuiWindowFlags;
using static UiDebug2.UiDebug2Plugin;

namespace UiDebug2;

// Customised version of https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Debugging/UIDebug.cs from https://github.com/aers/FFXIVUIDebug
internal partial class UiDebug2 : IDisposable
{
    private ElementSelector elementSelector;

    internal UiDebug2(IGameGui gameGui, IPluginLog pluginLog)
    {
        this.elementSelector = new(this);

        GameGui = gameGui;

        Log = pluginLog;

        PluginInterface.UiBuilder.Draw += DrawPopouts;
    }

    internal static IPluginLog Log { get; set; } = null!;

    internal static IGameGui GameGui { get; set; } = null!;

    internal static Dictionary<string, AddonTree> AddonTrees { get; set; } = new();

    internal static WindowSystem PopoutWindows { get; set; } = new("UiDebugPopouts");

    internal string? SelectedAddonName { get; set; }

    public void Dispose()
    {
        PopoutWindows.RemoveAllWindows();
        PluginInterface.UiBuilder.Draw -= DrawPopouts;
        this.elementSelector.Dispose();
    }

    internal static void DrawPopouts() => PopoutWindows.Draw();

    internal void Draw()
    {
        this.DrawSidebar();
        this.DrawMainPanel();
    }

    internal void DrawMainPanel()
    {
        ImGui.SameLine();
        ImGui.BeginChild("###uiDebugMainPanel", new(-1, -1), true, HorizontalScrollbar);

        if (this.SelectedAddonName != null)
        {
            var addonTree = AddonTree.GetOrCreate(this.SelectedAddonName);

            if (addonTree == null)
            {
                this.SelectedAddonName = null;
                return;
            }

            addonTree.Draw();
        }

        ImGui.EndChild();
    }
}

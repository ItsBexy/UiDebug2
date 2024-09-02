using System;
using System.Collections.Generic;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using UiDebug2.Browsing;

using static ImGuiNET.ImGuiWindowFlags;

namespace UiDebug2;

// Customised version of https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Debugging/UIDebug.cs from https://github.com/aers/FFXIVUIDebug
internal partial class UiDebug2 : IDisposable
{
    private readonly ElementSelector elementSelector;

    internal UiDebug2(IPluginLog pluginLog, IGameGui gameGui)
    {
        this.elementSelector = new(this);

        GameGui = gameGui;
        Log = pluginLog;
    }

    internal static IPluginLog Log { get; set; } = null!;

    internal static IGameGui GameGui { get; set; } = null!;

    internal static Dictionary<string, AddonTree> AddonTrees { get; set; } = new();

    internal static WindowSystem PopoutWindows { get; set; } = new("UiDebugPopouts");

    internal string? SelectedAddonName { get; set; }

    public void Dispose()
    {
        foreach (var a in AddonTrees)
        {
            a.Value.Dispose();
        }

        AddonTrees.Clear();
        PopoutWindows.RemoveAllWindows();
        this.elementSelector.Dispose();
    }

    internal void Draw()
    {
        PopoutWindows.Draw();
        this.DrawSidebar();
        this.DrawMainPanel();
    }

    private void DrawMainPanel()
    {
        ImGui.SameLine();
        ImGui.BeginChild("###uiDebugMainPanel", new(-1, -1), true, HorizontalScrollbar);

        if (this.elementSelector.Active)
        {
            this.elementSelector.DrawSelectorFrame();
        }
        else
        {
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
        }

        ImGui.EndChild();
    }
}

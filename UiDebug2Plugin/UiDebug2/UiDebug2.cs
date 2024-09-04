using System;
using System.Collections.Generic;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using UiDebug2.Browsing;

using static ImGuiNET.ImGuiWindowFlags;

namespace UiDebug2;

// Customised version of https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Debugging/UIDebug.cs from https://github.com/aers/FFXIVUIDebug

/// <summary>
/// A tool for browsing the contents and structure of UI elements.
/// </summary>
internal partial class UiDebug2 : IDisposable
{
    private readonly ElementSelector elementSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiDebug2"/> class.
    /// </summary>
    /// <param name="pluginLog">The log service.</param>
    /// <param name="gameGui">The gameGui service.</param>
    internal UiDebug2(IPluginLog pluginLog, IGameGui gameGui)
    {
        this.elementSelector = new(this);

        GameGui = gameGui;
        Log = pluginLog;
    }

    /// <inheritdoc cref="IPluginLog"/>
    internal static IPluginLog Log { get; set; } = null!;

    /// <inheritdoc cref="IGameGui"/>
    internal static IGameGui GameGui { get; set; } = null!;

    /// <summary>
    /// Gets a collection of <see cref="AddonTree"/> instances, each representing an <see cref="FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase"/>.
    /// </summary>
    internal static Dictionary<string, AddonTree> AddonTrees { get; } = [];

    /// <summary>
    /// Gets or sets a window system to handle any popout windows for addons or nodes.
    /// </summary>
    internal static WindowSystem PopoutWindows { get; set; } = new("UiDebugPopouts");

    /// <summary>
    /// Gets or sets the name of the currently-selected <see cref="AtkUnitBase"/>.
    /// </summary>
    internal string? SelectedAddonName { get; set; }

    /// <summary>
    /// Clears all windows and <see cref="AddonTree"/>s.
    /// </summary>
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

    /// <summary>
    /// Draws the UiDebug tool's interface and contents.
    /// </summary>
    internal void Draw()
    {
        PopoutWindows.Draw();
        this.DrawSidebar();
        this.DrawMainPanel();
    }

    private void DrawMainPanel()
    {
        ImGui.SameLine();
        var ch = ImRaii.Child("###uiDebugMainPanel", new(-1, -1), true, HorizontalScrollbar);

        if (this.elementSelector.Active)
        {
            this.elementSelector.DrawSelectorOutput();
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

        ch.Dispose();
    }
}

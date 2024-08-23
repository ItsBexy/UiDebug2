using System;
using System.Numerics;

using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using UiDebug2.Browsing;

using static UiDebug2.UiDebug2;

namespace UiDebug2;

internal unsafe class NodePopoutWindow : Window, IDisposable
{
    private readonly ResNodeTree resNodeTree;

    private bool firstDraw = true;

    public NodePopoutWindow(ResNodeTree nodeTree, string windowName)
        : base(windowName)
    {
        this.resNodeTree = nodeTree;

        var pos = ImGui.GetMousePos() + new Vector2(50, -50);
        var workSize = ImGui.GetMainViewport().WorkSize;
        var pos2 = new Vector2(Math.Min(workSize.X - 750, pos.X), Math.Min(workSize.Y - 250, pos.Y));

        this.Position = pos2;
        this.IsOpen = true;
        this.PositionCondition = ImGuiCond.Once;
        this.SizeCondition = ImGuiCond.Once;
        this.Size = new(700, 200);
        this.SizeConstraints = new() { MinimumSize = new(100, 100) };
    }

    public AddonTree AddonTree => this.resNodeTree.AddonTree;

    internal AtkResNode* Node => this.resNodeTree.Node;

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.Node != null && this.AddonTree.ContainsNode(this.Node))
        {
            ImGui.BeginChild($"{(nint)this.Node:X}popoutChild", new(-1, -1), true);
            ResNodeTree.GetOrCreate(this.Node, this.AddonTree).Print(null, forceOpen: this.firstDraw);
            ImGui.EndChild();
            this.firstDraw = false;
        }
        else
        {
            Log.Warning($"Popout closed ({this.WindowName}); Node or Addon no longer exists.");
            this.IsOpen = false;
            this.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}

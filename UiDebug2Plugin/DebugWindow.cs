using System;
using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace UiDebug2;

internal class DebugWindow : Window, IDisposable
{
    private readonly UiDebug2 uiDebug;

    internal DebugWindow(UiDebug2 panel)
        : base("UI Debug 2##uiDebug2Window")
    {
        this.Flags = ImGuiWindowFlags.None;
        this.Size = new Vector2(800, 500);
        this.SizeCondition = ImGuiCond.Once;

        this.uiDebug = panel;
    }

    public override void Draw() => this.uiDebug.Draw();

    public void Dispose()
    {
    }
}

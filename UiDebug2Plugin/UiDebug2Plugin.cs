using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using static UiDebug2.UiDebug2Plugin.Service;

namespace UiDebug2;

internal sealed class UiDebug2Plugin : IDalamudPlugin
{
    internal const string CommandName = "/uidebug2";

    internal static readonly WindowSystem WindowSystem = new("UI Debug 2");

    public UiDebug2Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;

        PluginInterface.Create<Service>();

        this.UiDebug2 = new(GameGui, Log);
        this.DebugWindow = new(this.UiDebug2);

        WindowSystem.AddWindow(this.DebugWindow);

        CommandManager.AddHandler(CommandName, new(this.OnCommand) { HelpMessage = "UI Debug 2", });

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUI;
    }

    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    internal DebugWindow DebugWindow { get; init; }

    internal UiDebug2 UiDebug2 { get; init; }

    public static void DrawUI() => WindowSystem.Draw();

    public void Dispose()
    {
        this.DebugWindow.Dispose();
        this.UiDebug2.Dispose();
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
    }

    public void OnCommand(string command, string args)
    {
        this.ToggleMainUI();
    }

    public void ToggleConfigUI() => this.DebugWindow.Toggle();

    public void ToggleMainUI() => this.DebugWindow.Toggle();

    public class Service
    {
        [PluginService]
        internal static ICommandManager CommandManager { get; private set; } = null!;

        [PluginService]
        internal static IPluginLog Log { get; private set; } = null!;

        [PluginService]
        internal static IGameGui GameGui { get; private set; } = null!;
    }
}

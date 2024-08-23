using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using UiDebug2.Utility;

using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;

namespace UiDebug2.Browsing;

public static class Events
{
    internal static unsafe void PrintEvents(AtkResNode* node)
    {
        var evt = node->AtkEventManager.Event;
        if (evt == null)
        {
            return;
        }

        if (ImGui.TreeNode($"Events##{(nint)node:X}eventTree"))
        {
            if (ImGui.BeginTable($"##{(nint)node:X}eventTable", 7, Resizable | SizingFixedFit | Borders | RowBg))
            {
                ImGui.TableSetupColumn("#", WidthFixed);
                ImGui.TableSetupColumn("Type", WidthFixed);
                ImGui.TableSetupColumn("Param", WidthFixed);
                ImGui.TableSetupColumn("Flags", WidthFixed);
                ImGui.TableSetupColumn("Unk29", WidthFixed);
                ImGui.TableSetupColumn("Target", WidthFixed);
                ImGui.TableSetupColumn("Listener", WidthFixed);

                ImGui.TableHeadersRow();

                var i = 0;
                while (evt != null)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i++}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->Type}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->Param}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->Flags}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{evt->Unk29}");
                    ImGui.TableNextColumn();
                    Gui.ClickToCopyText($"{(nint)evt->Target:X}");
                    ImGui.TableNextColumn();
                    Gui.ClickToCopyText($"{(nint)evt->Listener:X}");
                    evt = evt->NextEvent;
                }

                ImGui.EndTable();
            }

            ImGui.TreePop();
        }
    }
}

using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Utility.Util;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

internal unsafe partial class TextNodeTree : ResNodeTree
{
    internal TextNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private AtkTextNode* TxtNode => (AtkTextNode*)this.Node;

    private Utf8String NodeText => TxtNode->NodeText;

    private protected override void PrintNodeObject() => ShowStruct(this.TxtNode);

    private protected override void PrintFieldsForNodeType(bool editorOpen = false)
    {
        if (editorOpen)
        {
            return;
        }

        ImGui.TextColored(new(1), "Text:");
        ImGui.SameLine();

#pragma warning disable
        try
        {
            ImGuiHelpers.SeStringWrapped(NodeText.AsSpan(), new SeStringDrawParams { Color = TxtNode->TextColor.RGBA, EdgeColor = TxtNode->EdgeColor.RGBA, ForceEdgeColor = true, EdgeStrength = 1f });
        }
        catch
        {
            ImGui.Text(Marshal.PtrToStringAnsi(new(NodeText.StringPtr)) ?? "");
        }
#pragma warning restore

        PrintFieldValuePairs(
            ("Font", $"{TxtNode->FontType}"),
            ("Font Size", $"{TxtNode->FontSize}"),
            ("Alignment", $"{TxtNode->AlignmentType}"));

        PrintColor(TxtNode->TextColor, $"Text Color: {SwapEndianness(TxtNode->TextColor.RGBA):X8}");
        ImGui.SameLine();
        PrintColor(TxtNode->EdgeColor, $"Edge Color: {SwapEndianness(TxtNode->EdgeColor.RGBA):X8}");

        this.PrintPayloads();
    }

    private void PrintPayloads()
    {
        if (ImGui.TreeNode($"Text Payloads##{(nint)this.Node:X}"))
        {
            var utf8String = this.NodeText;
            var seStringBytes = new byte[utf8String.BufUsed];
            for (var i = 0L; i < utf8String.BufUsed; i++)
            {
                seStringBytes[i] = utf8String.StringPtr[i];
            }

            var seString = SeString.Parse(seStringBytes);
            for (var i = 0; i < seString.Payloads.Count; i++)
            {
                var payload = seString.Payloads[i];
                ImGui.Text($"[{i}]");
                ImGui.SameLine();
                switch (payload.Type)
                {
                    case PayloadType.RawText when payload is TextPayload tp:
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                        ImGui.Text("Raw Text: '");
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1));
                        ImGui.Text(tp.Text);
                        ImGui.PopStyleColor();
                        ImGui.SameLine();
                        ImGui.PopStyleVar();
                        ImGui.Text("'");
                        break;
                    }

                    default:
                    {
                        ImGui.Text(payload.ToString());
                        break;
                    }
                }
            }

            ImGui.TreePop();
        }
    }
}

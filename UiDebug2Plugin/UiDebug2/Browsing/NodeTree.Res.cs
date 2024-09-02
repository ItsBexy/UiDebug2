using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using UiDebug2.Utility;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.NodeFlags;
using static UiDebug2.Browsing.Events;
using static UiDebug2.ElementSelector;
using static UiDebug2.UiDebug2;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

internal unsafe partial class ResNodeTree : IDisposable
{
    private NodePopoutWindow? window;

    private bool editorOpen;

    private protected ResNodeTree(AtkResNode* node, AddonTree addonTree)
    {
        this.Node = node;
        this.AddonTree = addonTree;
        this.NodeType = node->Type;
        this.AddonTree.NodeTrees.Add((nint)this.Node, this);
    }

    protected internal AtkResNode* Node { get; set; }

    protected internal AddonTree AddonTree { get; private set; }

    private protected NodeType NodeType { get; init; }

    public void Dispose()
    {
        if (this.window != null && PopoutWindows.Windows.Contains(this.window))
        {
            PopoutWindows.RemoveWindow(this.window);
            this.window.Dispose();
        }
    }

    internal static ResNodeTree GetOrCreate(AtkResNode* node, AddonTree addonTree) =>
        addonTree.NodeTrees.TryGetValue((nint)node, out var nodeTree) ? nodeTree
            : (int)node->Type > 1000
                ? new ComponentNodeTree(node, addonTree)
                : node->Type switch
                {
                    NodeType.Text => new TextNodeTree(node, addonTree),
                    NodeType.Image => new ImageNodeTree(node, addonTree),
                    NodeType.NineGrid => new NineGridNodeTree(node, addonTree),
                    NodeType.ClippingMask => new ClippingMaskNodeTree(node, addonTree),
                    NodeType.Counter => new CounterNodeTree(node, addonTree),
                    NodeType.Collision => new CollisionNodeTree(node, addonTree),
                    _ => new ResNodeTree(node, addonTree),
                };

    internal static void PrintNodeList(AtkResNode** nodeList, int count, AddonTree addonTree)
    {
        for (uint j = 0; j < count; j++)
        {
            GetOrCreate(nodeList[j], addonTree).Print(j);
        }
    }

    internal static void PrintNodeListAsTree(AtkResNode** nodeList, int count, string label, AddonTree addonTree, Vector4 color)
    {
        if (count <= 0)
        {
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        var treeOpened = NestedTreePush($"{label}##{(nint)nodeList:X}", color, out var lineStart);
        ImGui.PopStyleColor();

        if (treeOpened)
        {
            PrintNodeList(nodeList, count, addonTree);
            NestedTreePop(lineStart, color);
        }
    }

    internal void Print(uint? index, bool forceOpen = false)
    {
        if (SearchResults.Length > 0 && SearchResults[0] == (nint)this.Node)
        {
            this.PrintWithHighlights(index);
        }
        else
        {
            this.PrintTree(index, forceOpen);
        }
    }

    internal void WriteTreeHeading()
    {
        ImGui.Text(this.GetHeaderText());
        this.PrintFieldNames();
    }

    private protected void PrintFieldName(nint ptr, Vector4 color)
    {
        if (this.AddonTree.FieldNames.TryGetValue(ptr, out var result))
        {
            ImGui.SameLine();
            ImGui.TextColored(color, string.Join(".", result));
        }
    }

    private protected virtual string GetHeaderText()
    {
        var count = this.GetDirectChildCount();
        return $"{this.NodeType} Node{(count > 0 ? $" [+{count}]" : string.Empty)} ({(nint)this.Node:X})";
    }

    private protected virtual void PrintNodeObject()
    {
        ShowStruct(this.Node);
        ImGui.SameLine();
        ImGui.NewLine();
    }

    private protected virtual void PrintFieldNames() => this.PrintFieldName((nint)this.Node, new(0, 0.85F, 1, 1));

    private protected virtual void PrintChildNodes()
    {
        var prevNode = Node->ChildNode;
        while (prevNode != null)
        {
            GetOrCreate(prevNode, this.AddonTree).Print(null);
            prevNode = prevNode->PrevSiblingNode;
        }
    }

    private protected virtual void PrintFieldsForNodeType(bool editorOpen = false)
    {
    }

    private int GetDirectChildCount()
    {
        var count = 0;
        if (this.Node->ChildNode != null)
        {
            count++;

            var prev = this.Node->ChildNode;
            while (prev->PrevSiblingNode != null)
            {
                prev = prev->PrevSiblingNode;
                count++;
            }
        }

        return count;
    }

    private void PrintWithHighlights(uint? index)
    {
        if (!Scrolled)
        {
            ImGui.SetScrollHereY();
            Scrolled = true;
        }

        var start = ImGui.GetCursorScreenPos() - new Vector2(5);
        this.PrintTree(index, true);
        var end = new Vector2(ImGui.GetMainViewport().WorkSize.X, ImGui.GetCursorScreenPos().Y + 5);

        ImGui.GetWindowDrawList().AddRectFilled(start, end, RgbaVector4ToUint(new Vector4(1, 1, 0.2f, 1) { W = Countdown / 200f }));
    }

    private void PrintTree(uint? index, bool forceOpen = false)
    {
        var visible = Node->NodeFlags.HasFlag(Visible);

        var displayColor = !visible ? new Vector4(0.8f, 0.8f, 0.8f, 1) :
                           Node->Color.A == 0 ? new(0.015f, 0.575f, 0.355f, 1) :
                           new(0.1f, 1f, 0.1f, 1f);

        if (forceOpen || SearchResults.Contains((nint)this.Node))
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
        }

        ImGui.PushStyleColor(ImGuiCol.Text, displayColor);

        var treePush = NestedTreePush($"{(index == null ? string.Empty : $"[{index}] ")}[#{Node->NodeId}]###{(nint)this.Node:X}nodeTree", displayColor, out var lineStart);

        if (ImGui.IsItemHovered())
        {
            new NodeBounds(this.Node).Draw(visible ? new(0.1f, 1f, 0.1f, 1f) : new(1f, 0f, 0.2f, 1f));
        }

        ImGui.SameLine();
        this.WriteTreeHeading();

        ImGui.PopStyleColor();

        if (treePush)
        {
            try
            {
                PrintFieldValuePair("Node", $"{(nint)this.Node:X}");

                ImGui.SameLine();
                this.PrintNodeObject();

                PrintFieldValuePairs(
                    ("NodeID", $"{Node->NodeId}"),
                    ("Type", $"{Node->Type}"));

                this.DrawBasicControls();

                if (this.editorOpen)
                {
                    this.DrawNodeEditorTable();
                }
                else
                {
                    this.PrintResNodeFields();
                }

                this.PrintFieldsForNodeType(this.editorOpen);
                PrintEvents(this.Node);
                new TimelineTree(this.Node).Print();

                this.PrintChildNodes();
            }
            catch (Exception ex)
            {
                ImGui.TextDisabled($"Couldn't display node!\n\n{ex}");
            }

            NestedTreePop(lineStart, displayColor);
        }
    }

    private void DrawBasicControls()
    {
        ImGui.SameLine();
        var y = ImGui.GetCursorPosY();

        ImGui.SetCursorPosY(y - 2);
        var isVisible = Node->NodeFlags.HasFlag(Visible);
        if (ImGuiComponents.IconButton("vis", isVisible ? Eye : EyeSlash, isVisible ? new Vector4(0.0f, 0.8f, 0.2f, 1f) : new(0.6f, 0.6f, 0.6f, 1)))
        {
            if (isVisible)
            {
                Node->NodeFlags &= ~Visible;
            }
            else
            {
                Node->NodeFlags |= Visible;
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Visibility");
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(y - 2);
        ImGui.Checkbox($"Edit###editCheckBox{(nint)this.Node}", ref this.editorOpen);

        ImGui.SameLine();
        ImGui.SetCursorPosY(y - 2);
        if (ImGuiComponents.IconButton($"###{(nint)this.Node}popoutButton", this.window?.IsOpen == true ? Times : ArrowUpRightFromSquare, null))
        {
            this.TogglePopout();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Popout Window");
        }
    }

    private void TogglePopout()
    {
        if (this.window != null)
        {
            this.window.IsOpen = !this.window.IsOpen;
        }
        else
        {
            this.window = new NodePopoutWindow(this, $"{this.AddonTree.AddonName}: {this.GetHeaderText()}###nodePopout{(nint)this.Node}");
            PopoutWindows.AddWindow(this.window);
        }
    }

    private void PrintResNodeFields()
    {
        PrintFieldValuePairs(
            ("X", $"{Node->X}"),
            ("Y", $"{Node->Y}"),
            ("Width", $"{Node->Width}"),
            ("Height", $"{Node->Height}"),
            ("Priority", $"{Node->Priority}"),
            ("Depth", $"{Node->Depth}"),
            ("DrawFlags", $"0x{Node->DrawFlags:X}"));

        PrintFieldValuePairs(
            ("ScaleX", $"{Node->ScaleX:F2}"),
            ("ScaleY", $"{Node->ScaleY:F2}"),
            ("OriginX", $"{Node->OriginX}"),
            ("OriginY", $"{Node->OriginY}"),
            ("Rotation", $"{Node->Rotation * (180d / Math.PI):F1}° / {Node->Rotation:F7}rad "));

        var color = Node->Color;
        var add = new Vector3(Node->AddRed, Node->AddGreen, Node->AddBlue);
        var multiply = new Vector3(Node->MultiplyRed, Node->MultiplyGreen, Node->MultiplyBlue);

        PrintColor(RgbaUintToVector4(color.RGBA) with { W = 1 }, $"RGB: {SwapEndianness(color.RGBA) >> 8:X6}");
        ImGui.SameLine();
        PrintColor(color, $"Alpha: {color.A}");
        ImGui.SameLine();
        PrintColor((add / new Vector3(510f)) + new Vector3(0.5f), $"Add: {add.X} {add.Y} {add.Z}");
        ImGui.SameLine();
        PrintColor(multiply / 255f, $"Multiply: {multiply.X} {multiply.Y} {multiply.Z}");

        PrintFieldValuePairs(("Flags", $"0x{(uint)Node->NodeFlags:X} ({Node->NodeFlags})"));
    }
}

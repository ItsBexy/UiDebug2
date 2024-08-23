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
using static UiDebug2.Browsing.Events;
using static UiDebug2.ElementSelector;
using static UiDebug2.UiDebug2;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

public unsafe partial class ResNodeTree : IDisposable
{
    private NodePopoutWindow? window;

    private bool editable;

    internal ResNodeTree(AtkResNode* node, AddonTree addonTree)
    {
        this.Node = node;
        this.AddonTree = addonTree;
        this.NodeType = node->Type;
        this.DirectChildCount = this.GetDirectChildCount();
        this.TypeString = $"{this.NodeType} Node{(this.DirectChildCount > 0 ? $" [+{this.DirectChildCount}]" : string.Empty)}";
        this.PointerString = $"({(long)node:X})";
        this.AddonTree.NodeTrees.Add(this.NodePtr, this);
    }

    internal AtkResNode* Node { get; set; }

    internal nint NodePtr => (nint)this.Node;

    internal NodeType NodeType { get; init; }

    internal string TypeString { get; init; }

    internal string PointerString { get; init; }

    internal AddonTree AddonTree { get; set; }

    internal int DirectChildCount { get; init; }

    /// <inheritdoc/>
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

    internal int GetDirectChildCount()
    {
        var count = 0;
        if (Node->ChildNode != null)
        {
            count++;

            var prev = Node->ChildNode;
            while (prev->PrevSiblingNode != null)
            {
                prev = prev->PrevSiblingNode;
                count++;
            }
        }

        return count;
    }

    internal virtual void PrintNodeObject()
    {
        ShowStruct(this.Node);
        ImGui.SameLine();
        ImGui.NewLine();
    }

    internal void Print(uint? index, bool forceOpen = false)
    {
        if (SearchResults.Length > 0 && SearchResults[0] == this.NodePtr)
        {
            this.PrintWithHighlights(index);
        }
        else
        {
            this.PrintTree(index, forceOpen);
        }
    }

    internal void PrintWithHighlights(uint? index)
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

    internal void PrintTree(uint? index, bool forceOpen = false)
    {
        var visible = Node->IsVisible();

        var displayColor = !visible ? new Vector4(0.8f, 0.8f, 0.8f, 1) :
                           Node->Color.A == 0 ? new(0.015f, 0.575f, 0.355f, 1) :
                           new(0.1f, 1f, 0.1f, 1f);

        if (forceOpen || SearchResults.Contains(this.NodePtr))
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
        }

        ImGui.PushStyleColor(ImGuiCol.Text, displayColor);

        var treePush = NestedTreePush($"{(index == null ? string.Empty : $"[{index}] ")}[#{Node->NodeId}]###{this.NodePtr:X}nodeTree", displayColor, out var lineStart);

        if (ImGui.IsItemHovered())
        {
            new NodeBounds(this.Node).Draw(visible ? new(0.1f, 1f, 0.1f, 1f) : new(1f, 0f, 0.2f, 1f));
        }

        ImGui.SameLine();
        this.WriteTreeHeading();

        ImGui.PopStyleColor();

        if (treePush)
        {
            PrintFieldValuePair("Node", $"{this.NodePtr:X}");

            ImGui.SameLine();
            this.PrintNodeObject();

            PrintFieldValuePairs(
                ("NodeID", $"{Node->NodeId}"),
                ("Type", $"{Node->Type}"));

            this.DrawBasicControls();

            if (this.editable)
            {
                this.DrawNodeEditorTable();
            }
            else
            {
                this.PrintResNodeFields();
            }

            this.PrintFieldsForNodeType(this.editable);
            PrintEvents(this.Node);
            new TimelineTree(this.Node).Print();

            this.PrintChildNodes();

            NestedTreePop(lineStart, displayColor);
        }
    }

    internal void DrawBasicControls()
    {
        ImGui.SameLine();
        var y = ImGui.GetCursorPosY();

        ImGui.SetCursorPosY(y - 2);
        if (ImGuiComponents.IconButton("vis", Node->IsVisible() ? Eye : EyeSlash, Node->IsVisible() ? new Vector4(0.0f, 0.8f, 0.2f, 1f) : new(0.6f, 0.6f, 0.6f, 1)))
        {
            Node->ToggleVisibility(!Node->IsVisible());
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Visibility");
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(y - 2);
        ImGui.Checkbox($"Edit###editCheckBox{this.NodePtr}", ref this.editable);

        ImGui.SameLine();
        ImGui.SetCursorPosY(y - 2);
        if (ImGuiComponents.IconButton($"###{this.NodePtr}popoutButton", this.window?.IsOpen == true ? Times : ArrowUpRightFromSquare, null))
        {
            this.TogglePopout();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Popout Window");
        }
    }

    internal void WriteTreeHeading()
    {
        ImGui.Text(this.TypeString);
        ImGui.SameLine();
        ImGui.Text(this.PointerString);

        this.PrintFieldNames();
    }

    internal virtual void PrintFieldNames() => this.PrintFieldName(this.NodePtr, new(0, 0.85F, 1, 1));

    internal void PrintFieldName(nint ptr, Vector4 color)
    {
        if (this.AddonTree.FieldNames.TryGetValue(ptr, out var result))
        {
            ImGui.SameLine();
            ImGui.TextColored(color, string.Join(".", result));
        }
    }

    internal virtual void PrintChildNodes()
    {
        var prevNode = Node->ChildNode;
        while (prevNode != null)
        {
            GetOrCreate(prevNode, this.AddonTree).Print(null);
            prevNode = prevNode->PrevSiblingNode;
        }
    }

    internal virtual void PrintFieldsForNodeType(bool editorOpen = false)
    {
    }

    internal void TogglePopout()
    {
        if (this.window != null)
        {
            this.window.IsOpen = !this.window.IsOpen;
        }
        else
        {
            this.window = new NodePopoutWindow(this, $"{this.AddonTree.AddonName}: {this.TypeString} {this.PointerString}###nodePopout{this.NodePtr}");
            PopoutWindows.AddWindow(this.window);
        }
    }

    internal void PrintResNodeFields()
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

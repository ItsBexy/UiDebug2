using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Components;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;
using UiDebug2.Browsing;
using UiDebug2.Utility;

using static System.Globalization.NumberFormatInfo;

using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.UiBuilder;
using static Dalamud.Interface.Utility.ImGuiHelpers;
using static FFXIVClientStructs.FFXIV.Component.GUI.NodeFlags;
using static ImGuiNET.ImGuiCol;
using static ImGuiNET.ImGuiWindowFlags;
using static UiDebug2.UiDebug2;

#pragma warning disable CS0659

namespace UiDebug2;

internal unsafe class ElementSelector : IDisposable
{
    internal const int UnitListCount = 18;

    private readonly UiDebug2 uiDebug2;

    private string addressSearchInput = string.Empty;

    private int index;

    internal ElementSelector(UiDebug2 uiDebug2)
    {
        this.uiDebug2 = uiDebug2;
    }

    internal static nint[] SearchResults { get; set; } = Array.Empty<nint>();

    internal static float Countdown { get; set; }

    internal static bool Scrolled { get; set; }

    internal bool Active { get; set; }

    public void Dispose()
    {
        this.Active = false;
    }

    internal void DrawInterface()
    {
        ImGui.BeginChild("###sidebar_elementSelector", new(250, 0), true);

        ImGui.PushFont(IconFont);
        ImGui.PushStyleColor(Text, this.Active ? new Vector4(1, 1, 0.2f, 1) : new(1));
        if (ImGui.Button($"{(char)ObjectUngroup}"))
        {
            this.Active = !this.Active;
        }

        if (Countdown > 0)
        {
            Countdown -= 1;
            if (Countdown < 0)
            {
                Countdown = 0;
            }
        }

        ImGui.PopStyleColor();
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Element Selector");
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 32);
        ImGui.InputTextWithHint("###addressSearchInput", "Address Search", ref this.addressSearchInput, 18, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton("###elemSelectorAddrSearch", Search) && nint.TryParse(this.addressSearchInput, NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, InvariantInfo, out var address))
        {
            this.PerformSearch(address);
        }

        ImGui.EndChild();
    }

    internal void DrawSelectorFrame()
    {
        ImGui.GetIO().WantCaptureKeyboard = true;
        ImGui.GetIO().WantCaptureMouse = true;
        ImGui.GetIO().WantTextInput = true;
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            this.Active = false;
            return;
        }

        ImGui.Text("ELEMENT SELECTOR");
        ImGui.TextDisabled("Use the mouse to hover and identify UI elements, then click to jump to them in the inspector");
        ImGui.TextDisabled("Use the scrollwheel to choose between overlapping elements");
        ImGui.TextDisabled("Press ESCAPE to cancel");
        ImGui.Spacing();

        var mousePos = ImGui.GetMousePos() - MainViewport.Pos;
        var addonResults = GetAtkUnitBaseAtPosition(mousePos);

        ImGui.PushStyleColor(WindowBg, new Vector4(0.5f));
        ImGui.BeginChild("noClick", new(800, 2000), false, NoInputs | NoBackground | NoScrollWithMouse);
        ImGui.BeginGroup();

        Gui.PrintFieldValuePair("Mouse Position", $"{mousePos.X}, {mousePos.Y}");
        ImGui.Spacing();
        ImGui.Text("RESULTS:\n");

        var i = 0;
        foreach (var a in addonResults)
        {
            var name = a.Addon->NameString;
            ImGui.Text($"[Addon] {name}");
            ImGui.Indent(15);
            foreach (var n in a.Nodes)
            {
                var nSelected = i++ == this.index;

                PrintNodeHeaderOnly(n.Node, nSelected, a.Addon);

                if (nSelected && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    this.Active = false;

                    this.uiDebug2.SelectedAddonName = a.Addon->NameString;

                    var ptrList = new List<nint> { (nint)n.Node };

                    var nextNode = n.Node->ParentNode;
                    while (nextNode != null)
                    {
                        ptrList.Add((nint)nextNode);
                        nextNode = nextNode->ParentNode;
                    }

                    SearchResults = ptrList.ToArray();
                    Countdown = 100;
                    Scrolled = false;
                }

                if (nSelected)
                {
                    n.NodeBounds.DrawFilled(new(1, 1, 0.2f, 1));
                }
            }

            ImGui.Indent(-15);
        }

        if (i != 0)
        {
            this.index -= (int)ImGui.GetIO().MouseWheel;
            while (this.index < 0)
            {
                this.index += i;
            }

            while (this.index >= i)
            {
                this.index -= i;
            }
        }

        ImGui.EndGroup();
        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static List<AddonResult> GetAtkUnitBaseAtPosition(Vector2 position)
    {
        var addonResults = new List<AddonResult>();
        var unitListBaseAddr = GetUnitListBaseAddr();
        if (unitListBaseAddr == null)
        {
            return addonResults;
        }

        foreach (var unit in UnitListOptions)
        {
            var unitManager = &unitListBaseAddr[unit.Index];

            var safeCount = Math.Min(unitManager->Count, unitManager->Entries.Length);

            for (var i = 0; i < safeCount; i++)
            {
                var addon = unitManager->Entries[i].Value;

                if (addon == null || addon->RootNode == null)
                {
                    continue;
                }

                if (!addon->IsVisible || !addon->RootNode->NodeFlags.HasFlag(Visible))
                {
                    continue;
                }

                var addonResult = new AddonResult(addon, new());

                if (addonResults.Contains(addonResult))
                {
                    continue;
                }

                if (addon->X > position.X || addon->Y > position.Y)
                {
                    continue;
                }

                if (addon->X + addon->RootNode->Width < position.X)
                {
                    continue;
                }

                if (addon->Y + addon->RootNode->Height < position.Y)
                {
                    continue;
                }

                addonResult.Nodes.AddRange(GetNodeAtPosition(&addon->UldManager, position, true));
                addonResults.Add(addonResult);
            }
        }

        return addonResults.OrderBy(static w => w.Area).ToList();
    }

    private static List<NodeResult> GetNodeAtPosition(AtkUldManager* uldManager, Vector2 position, bool reverse)
    {
        var nodeResults = new List<NodeResult>();
        for (var i = 0; i < uldManager->NodeListCount; i++)
        {
            var node = uldManager->NodeList[i];

            var bounds = new NodeBounds(node);

            if (!bounds.ContainsPoint(position))
            {
                continue;
            }

            if ((int)node->Type >= 1000)
            {
                var compNode = (AtkComponentNode*)node;
                nodeResults.AddRange(GetNodeAtPosition(&compNode->Component->UldManager, position, false));
            }

            nodeResults.Add(new() { NodeBounds = bounds, Node = node });
        }

        if (reverse)
        {
            nodeResults.Reverse();
        }

        return nodeResults;
    }

    private static bool FindByAddress(AtkUnitBase* atkUnitBase, nint address)
    {
        if (atkUnitBase->RootNode == null)
        {
            return false;
        }

        if (!FindByAddress(atkUnitBase->RootNode, address, out var path))
        {
            return false;
        }

        Scrolled = false;
        SearchResults = path?.ToArray() ?? Array.Empty<nint>();
        Countdown = 100;
        return true;
    }

    private static bool FindByAddress(AtkResNode* node, nint address, out List<nint>? path)
    {
        if (node == null)
        {
            path = null;
            return false;
        }

        if ((nint)node == address)
        {
            path = new() { (nint)node };
            return true;
        }

        if ((int)node->Type >= 1000)
        {
            var cNode = (AtkComponentNode*)node;

            if (cNode->Component != null)
            {
                if ((nint)cNode->Component == address)
                {
                    path = new() { (nint)node };
                    return true;
                }

                if (FindByAddress(cNode->Component->UldManager.RootNode, address, out path) && path != null)
                {
                    path.Add((nint)node);
                    return true;
                }
            }
        }

        if (FindByAddress(node->ChildNode, address, out path) && path != null)
        {
            path.Add((nint)node);
            return true;
        }

        if (FindByAddress(node->PrevSiblingNode, address, out path) && path != null)
        {
            return true;
        }

        path = null;
        return false;
    }

    private static void PrintNodeHeaderOnly(AtkResNode* node, bool selected, AtkUnitBase* addon)
    {
        if (addon == null)
        {
            return;
        }

        if (node == null)
        {
            return;
        }

        var tree = AddonTree.GetOrCreate(addon->NameString);
        if (tree == null)
        {
            return;
        }

        ImGui.PushStyleColor(Text, selected ? new Vector4(1, 1, 0.2f, 1) : new(0.6f, 0.6f, 0.6f, 1));
        ResNodeTree.GetOrCreate(node, tree).WriteTreeHeading();
        ImGui.PopStyleColor();
    }

    private void PerformSearch(nint address)
    {
        var unitListBaseAddr = GetUnitListBaseAddr();
        if (unitListBaseAddr == null)
        {
            return;
        }

        for (var i = 0; i < UnitListCount; i++)
        {
            var unitManager = &unitListBaseAddr[i];
            var safeCount = Math.Min(unitManager->Count, unitManager->Entries.Length);

            for (var j = 0; j < safeCount; j++)
            {
                var addon = unitManager->Entries[j].Value;
                if ((nint)addon == address || FindByAddress(addon, address))
                {
                    this.uiDebug2.SelectedAddonName = addon->NameString;
                    return;
                }
            }
        }
    }

    internal struct AddonResult
    {
        internal AtkUnitBase* Addon;
        internal List<NodeResult> Nodes;
        internal float Area;

        public AddonResult(AtkUnitBase* addon, List<NodeResult> nodes)
        {
            this.Addon = addon;
            this.Nodes = nodes;
            var rootNode = addon->RootNode;
            this.Area = rootNode != null ? rootNode->Width * rootNode->Height * rootNode->ScaleY * rootNode->ScaleX : 0;
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is not AddonResult ar)
            {
                return false;
            }

            return (nint)this.Addon == (nint)ar.Addon;
        }
    }

    internal struct NodeResult
    {
        internal AtkResNode* Node;
        internal NodeBounds NodeBounds;

        public override readonly bool Equals(object? obj)
        {
            if (obj is not NodeResult nr)
            {
                return false;
            }

            return nr.Node == this.Node;
        }
    }
}

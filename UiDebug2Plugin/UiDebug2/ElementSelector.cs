using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImGuiScene;
using UiDebug2.Browsing;
using UiDebug2.Utility;

using static System.Globalization.NumberFormatInfo;
using static System.Reflection.BindingFlags;

using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.UiBuilder;
using static Dalamud.Interface.Utility.ImGuiHelpers;
using static FFXIVClientStructs.FFXIV.Component.GUI.NodeFlags;
using static ImGuiNET.ImGuiCol;
using static ImGuiNET.ImGuiWindowFlags;
using static UiDebug2.UiDebug2;
using static UiDebug2.UiDebug2Plugin;

#pragma warning disable CS0659

namespace UiDebug2;

internal unsafe class ElementSelector : IDisposable
{
    private readonly UiDebug2 uiDebug2;

    private string addressSearchInput = string.Empty;

    private bool active;

    private int index;

    internal ElementSelector(UiDebug2 uiDebug2)
    {
        this.uiDebug2 = uiDebug2;
    }

    internal static nint[] SearchResults { get; set; } = Array.Empty<nint>();

    internal static RawDX11Scene.BuildUIDelegate? OriginalHandler { get; set; }

    internal static float Countdown { get; set; }

    internal static bool Scrolled { get; set; }

    public void Dispose()
    {
        FreeExclusiveDraw();
    }

    internal static void SetExclusiveDraw(Action action)
    {
        // Possibly the most cursed shit I've ever done.
        if (OriginalHandler != null)
        {
            return;
        }

        try
        {
            var dalamudAssembly = PluginInterface.GetType().Assembly;
            var service1T = dalamudAssembly.GetType("Dalamud.Service`1");
            var interfaceManagerT = dalamudAssembly.GetType("Dalamud.Interface.Internal.InterfaceManager");
            if (service1T == null)
            {
                return;
            }

            if (interfaceManagerT == null)
            {
                return;
            }

            var serviceInterfaceManager = service1T.MakeGenericType(interfaceManagerT);
            var getter = serviceInterfaceManager.GetMethod("Get", Static | Public);
            if (getter == null)
            {
                return;
            }

            var interfaceManager = getter.Invoke(null, null);
            if (interfaceManager == null)
            {
                return;
            }

            var ef = interfaceManagerT.GetField("Draw", Instance | NonPublic);
            if (ef == null)
            {
                return;
            }

            if (ef.GetValue(interfaceManager) is not RawDX11Scene.BuildUIDelegate handler)
            {
                return;
            }

            OriginalHandler = handler;
            ef.SetValue(interfaceManager, new RawDX11Scene.BuildUIDelegate(action));
        }
        catch (Exception ex)
        {
            Log.Fatal($"{ex}");
        }
    }

    internal static void FreeExclusiveDraw()
    {
        if (OriginalHandler == null)
        {
            return;
        }

        try
        {
            var dalamudAssembly = PluginInterface.GetType().Assembly;
            var service1T = dalamudAssembly.GetType("Dalamud.Service`1");
            var interfaceManagerT = dalamudAssembly.GetType("Dalamud.Interface.Internal.InterfaceManager");
            if (service1T == null)
            {
                return;
            }

            if (interfaceManagerT == null)
            {
                return;
            }

            var serviceInterfaceManager = service1T.MakeGenericType(interfaceManagerT);
            var getter = serviceInterfaceManager.GetMethod("Get", Static | Public);
            if (getter == null)
            {
                return;
            }

            var interfaceManager = getter.Invoke(null, null);
            if (interfaceManager == null)
            {
                return;
            }

            var ef = interfaceManagerT.GetField("Draw", Instance | NonPublic);
            if (ef == null)
            {
                return;
            }

            ef.SetValue(interfaceManager, OriginalHandler);
            OriginalHandler = null;
        }
        catch (Exception ex)
        {
            Log.Fatal($"{ex}");
        }
    }

    internal static IEnumerable<AddonResult> GetAtkUnitBaseAtPosition(Vector2 position)
    {
        var addonResults = new List<AddonResult>();
        var unitListBaseAddr = &AtkStage.Instance()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;

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

                var addonResult = new AddonResult { Addon = addon, Nodes = new() };

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

        return addonResults.OrderBy(static w => w.Addon->GetScaledWidth(true) * w.Addon->GetScaledHeight(true));
    }

    internal static IEnumerable<NodeResult> GetNodeAtPosition(AtkUldManager* uldManager, Vector2 position, bool reverse)
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

    internal static bool FindByAddress(AtkUnitBase* atkUnitBase, nint address)
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

    internal static bool FindByAddress(AtkResNode* node, nint address, out List<nint>? path)
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

    internal static void PrintNodeHeaderOnly(AtkResNode* node, bool selected, AtkUnitBase* addon)
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

    internal void Draw()
    {
        ImGui.GetIO().WantCaptureKeyboard = true;
        ImGui.GetIO().WantCaptureMouse = true;
        ImGui.GetIO().WantTextInput = true;
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            this.active = false;
            FreeExclusiveDraw();
            return;
        }

        SetNextWindowPosRelativeMainViewport(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
        ImGui.SetNextWindowBgAlpha(0.3f);
        ForceNextWindowMainViewport();

        ImGui.Begin("ElementSelectorWindow", NoDecoration | NoScrollWithMouse | NoScrollbar);
        var drawList = ImGui.GetWindowDrawList();

        var y = 100f;
        foreach (var s in new[] { "Select an Element", "Press ESCAPE to cancel" })
        {
            var size = ImGui.CalcTextSize(s);
            var x = (ImGui.GetWindowWidth() / 2f) - (size.X / 2);
            drawList.AddText(new(x, y), Dalamud.Interface.ColorHelpers.RgbaVector4ToUint(new(1)), s);

            y += size.Y;
        }

        var mousePos = ImGui.GetMousePos() - MainViewport.Pos;
        var addonResults = GetAtkUnitBaseAtPosition(mousePos);

        ImGui.SetCursorPosX(100);
        ImGui.SetCursorPosY(100);
        ImGui.BeginChild("noClick", new(800, 2000), false, NoInputs | NoBackground | NoScrollWithMouse);
        ImGui.BeginGroup();

        ImGui.Text($"Mouse Position: {mousePos.X}, {mousePos.Y}\n");
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
                    this.active = false;
                    FreeExclusiveDraw();

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
        ImGui.End();
    }

    internal void PerformSearch(nint address)
    {
        var stage = AtkStage.Instance();

        var unitListBaseAddr = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;

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

    internal void DrawInterface()
    {
        ImGui.BeginChild("###sidebar_elementSelector", new(250, 0), true);

        ImGui.PushFont(IconFont);
        ImGui.PushStyleColor(Text, this.active ? new Vector4(1, 1, 0.2f, 1) : new(1));
        if (ImGui.Button($"{(char)ObjectUngroup}"))
        {
            this.active = !this.active;
            PluginInterface.UiBuilder.Draw -= this.Draw;
            FreeExclusiveDraw();

            if (this.active)
            {
                SetExclusiveDraw(this.Draw);
            }
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

    internal struct AddonResult
    {
        internal AtkUnitBase* Addon;
        internal List<NodeResult> Nodes;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

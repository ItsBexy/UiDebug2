using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.ComponentType;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

internal unsafe class ComponentNodeTree : ResNodeTree
{
    private readonly AtkUldManager* uldManager;

    private readonly ComponentType componentType;

    private readonly AtkComponentBase* component;

    internal ComponentNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
        this.component = ((AtkComponentNode*)node)->Component;
        this.uldManager = &component->UldManager;
        this.NodeType = 0;
        this.componentType = ((AtkUldComponentInfo*)uldManager->Objects)->ComponentType;

        var children = uldManager->NodeListCount;
        this.TypeString = $"{this.componentType} Component Node" + (children > 0 ? $" [+{children}]" : string.Empty);
        this.PointerString = $"(Node: {(long)node:X} / Comp: {(long)this.component:X})";
    }

    internal nint CompPtr => (nint)this.component;

    /// <inheritdoc/>
    internal override void PrintNodeObject()
    {
        base.PrintNodeObject();
        this.PrintComponentObject();
        ImGui.SameLine();
        ImGui.NewLine();
        this.PrintComponentDataObject();
        ImGui.SameLine();
        ImGui.NewLine();
    }

    /// <inheritdoc/>
    internal override void PrintChildNodes()
    {
        base.PrintChildNodes();
        var count = this.uldManager->NodeListCount;
        PrintNodeListAsTree(this.uldManager->NodeList, count, $"Node List [{count}]:", this.AddonTree, new(0f, 0.5f, 0.8f, 1f));
    }

    /// <inheritdoc/>
    internal override void PrintFieldNames()
    {
        this.PrintFieldName(this.NodePtr, new(0, 0.85F, 1, 1));
        this.PrintFieldName(this.CompPtr, new(0f, 0.5f, 0.8f, 1f));
    }

    internal void PrintComponentObject()
    {
        PrintFieldValuePair("Component", $"{this.CompPtr:X}");

        ImGui.SameLine();

        switch (this.componentType)
        {
            case Button:
                ShowStruct((AtkComponentButton*)this.component);
                break;
            case Slider:
                ShowStruct((AtkComponentSlider*)this.component);
                break;
            case Window:
                ShowStruct((AtkComponentWindow*)this.component);
                break;
            case CheckBox:
                ShowStruct((AtkComponentCheckBox*)this.component);
                break;
            case GaugeBar:
                ShowStruct((AtkComponentGaugeBar*)this.component);
                break;
            case RadioButton:
                ShowStruct((AtkComponentRadioButton*)this.component);
                break;
            case TextInput:
                ShowStruct((AtkComponentTextInput*)this.component);
                break;
            case Icon:
                ShowStruct((AtkComponentIcon*)this.component);
                break;
            case NumericInput:
                ShowStruct((AtkComponentNumericInput*)this.component);
                break;
            case List:
                ShowStruct((AtkComponentList*)this.component);
                break;
            case TreeList:
                ShowStruct((AtkComponentTreeList*)this.component);
                break;
            case DropDownList:
                ShowStruct((AtkComponentDropDownList*)this.component);
                break;
            case ScrollBar:
                ShowStruct((AtkComponentScrollBar*)this.component);
                break;
            case ListItemRenderer:
                ShowStruct((AtkComponentListItemRenderer*)this.component);
                break;
            case IconText:
                ShowStruct((AtkComponentIconText*)this.component);
                break;
            case DragDrop:
                ShowStruct((AtkComponentDragDrop*)this.component);
                break;
            case GuildLeveCard:
                ShowStruct((AtkComponentGuildLeveCard*)this.component);
                break;
            case TextNineGrid:
                ShowStruct((AtkComponentTextNineGrid*)this.component);
                break;
            case JournalCanvas:
                ShowStruct((AtkComponentJournalCanvas*)this.component);
                break;
            case HoldButton:
                ShowStruct((AtkComponentHoldButton*)this.component);
                break;
            case Portrait:
                ShowStruct((AtkComponentPortrait*)this.component);
                break;
            default:
                ShowStruct(this.component);
                break;
        }
    }

    internal void PrintComponentDataObject()
    {
        var componentData = component->UldManager.ComponentData;
        PrintFieldValuePair("Data", $"{(nint)componentData:X}");

        if (componentData != null)
        {
            ImGui.SameLine();
            switch (this.componentType)
            {
                case Base:
                    ShowStruct(componentData);
                    break;
                case Button:
                    ShowStruct((AtkUldComponentDataButton*)componentData);
                    break;
                case Window:
                    ShowStruct((AtkUldComponentDataWindow*)componentData);
                    break;
                case CheckBox:
                    ShowStruct((AtkUldComponentDataCheckBox*)componentData);
                    break;
                case RadioButton:
                    ShowStruct((AtkUldComponentDataRadioButton*)componentData);
                    break;
                case GaugeBar:
                    ShowStruct((AtkUldComponentDataGaugeBar*)componentData);
                    break;
                case Slider:
                    ShowStruct((AtkUldComponentDataSlider*)componentData);
                    break;
                case TextInput:
                    ShowStruct((AtkUldComponentDataTextInput*)componentData);
                    break;
                case NumericInput:
                    ShowStruct((AtkUldComponentDataNumericInput*)componentData);
                    break;
                case List:
                    ShowStruct((AtkUldComponentDataList*)componentData);
                    break;
                case DropDownList:
                    ShowStruct((AtkUldComponentDataDropDownList*)componentData);
                    break;
                case Tab:
                    ShowStruct((AtkUldComponentDataTab*)componentData);
                    break;
                case TreeList:
                    ShowStruct((AtkUldComponentDataTreeList*)componentData);
                    break;
                case ScrollBar:
                    ShowStruct((AtkUldComponentDataScrollBar*)componentData);
                    break;
                case ListItemRenderer:
                    ShowStruct((AtkUldComponentDataListItemRenderer*)componentData);
                    break;
                case Icon:
                    ShowStruct((AtkUldComponentDataIcon*)componentData);
                    break;
                case IconText:
                    ShowStruct((AtkUldComponentDataIconText*)componentData);
                    break;
                case DragDrop:
                    ShowStruct((AtkUldComponentDataDragDrop*)componentData);
                    break;
                case GuildLeveCard:
                    ShowStruct((AtkUldComponentDataGuildLeveCard*)componentData);
                    break;
                case TextNineGrid:
                    ShowStruct((AtkUldComponentDataTextNineGrid*)componentData);
                    break;
                case JournalCanvas:
                    ShowStruct((AtkUldComponentDataJournalCanvas*)componentData);
                    break;
                case Multipurpose:
                    ShowStruct((AtkUldComponentDataMultipurpose*)componentData);
                    break;
                case Map:
                    ShowStruct((AtkUldComponentDataMap*)componentData);
                    break;
                case Preview:
                    ShowStruct((AtkUldComponentDataPreview*)componentData);
                    break;
                case HoldButton:
                    ShowStruct((AtkUldComponentDataHoldButton*)componentData);
                    break;
                case Portrait:
                    ShowStruct((AtkUldComponentDataPortrait*)componentData);
                    break;
                default:
                    ShowStruct(componentData);
                    break;
            }
        }
    }

    /// <inheritdoc/>
    internal override void PrintFieldsForNodeType(bool editorOpen = false)
    {
        if (this.component == null)
        {
            return;
        }

        switch (this.componentType)
        {
            case TextInput:
                var textInputComponent = (AtkComponentTextInput*)this.component;
                ImGui.Text(
                    $"InputBase Text1: {Marshal.PtrToStringAnsi(new(textInputComponent->AtkComponentInputBase.UnkText1.StringPtr))}");
                ImGui.Text(
                    $"InputBase Text2: {Marshal.PtrToStringAnsi(new(textInputComponent->AtkComponentInputBase.UnkText2.StringPtr))}");
                ImGui.Text(
                    $"Text1: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText01.StringPtr))}");
                ImGui.Text(
                    $"Text2: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText02.StringPtr))}");
                ImGui.Text(
                    $"Text3: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText03.StringPtr))}");
                ImGui.Text(
                    $"Text4: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText04.StringPtr))}");
                ImGui.Text(
                    $"Text5: {Marshal.PtrToStringAnsi(new(textInputComponent->UnkText05.StringPtr))}");
                break;
            case List:
            case TreeList:
                var l = (AtkComponentList*)this.component;
                if (ImGui.SmallButton("Inc.Selected"))
                {
                    l->SelectedItemIndex++;
                }

                break;
            default:
                break;
        }
    }
}

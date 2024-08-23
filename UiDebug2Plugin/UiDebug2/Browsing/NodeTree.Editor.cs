using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using UiDebug2.Utility;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.Utility.ImGuiHelpers;
using static ImGuiNET.ImGuiColorEditFlags;
using static ImGuiNET.ImGuiInputTextFlags;
using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

public unsafe partial class ResNodeTree
{
    internal void DrawNodeEditorTable()
    {
        ImGui.BeginTable($"##Editor{(nint)this.Node}", 2, SizingStretchProp | NoHostExtendX | PadOuterX);

        this.DrawEditorRows();

        ImGui.EndTable();
    }

    protected virtual void DrawEditorRows()
    {
        var pos = new Vector2(Node->X, Node->Y);
        var size = new Vector2(Node->Width, Node->Height);
        var scale = new Vector2(Node->ScaleX, Node->ScaleY);
        var origin = new Vector2(Node->OriginX, Node->OriginY);
        var angle = (float)((Node->Rotation * (180 / Math.PI)) + 360);

        var rgba = RgbaUintToVector4(Node->Color.RGBA);
        var mult = new Vector3(Node->MultiplyRed, Node->MultiplyGreen, Node->MultiplyBlue) / 255f;
        var add = new Vector3(Node->AddRed, Node->AddGreen, Node->AddBlue);

        var hov = false;

        ImGui.TableSetupColumn("Labels", WidthFixed);
        ImGui.TableSetupColumn("Editors", WidthFixed);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Position:");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{this.NodePtr:X}position", ref pos))
        {
            Node->SetPositionFloat(pos.X, pos.Y);
        }

        hov |= SplitTooltip("X", "Y") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Size:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{this.NodePtr:X}size", ref size))
        {
            Node->SetWidth((ushort)Math.Max(size.X, 0));
            Node->SetHeight((ushort)Math.Max(size.Y, 0));
        }

        hov |= SplitTooltip("Width", "Height") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Scale:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{this.NodePtr:X}scale", ref scale, 0.05f))
        {
            Node->SetScale(scale.X, scale.Y);
            Node->DrawFlags |= 0xD;
        }

        hov |= SplitTooltip("ScaleX", "ScaleY") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Origin:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{this.NodePtr:X}origin", ref origin, 1))
        {
            Node->OriginX = origin.X;
            Node->OriginY = origin.Y;
            Node->DrawFlags |= 0xD;
        }

        hov |= SplitTooltip("OriginX", "OriginY") || ImGui.IsItemActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Rotation:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        while (angle > 180)
        {
            angle -= 360;
        }

        if (ImGui.DragFloat($"##{this.NodePtr:X}rotation", ref angle))
        {
            Node->Rotation = (float)(angle / (180 / Math.PI));
            Node->DrawFlags |= 0xD;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Rotation (deg)");
            hov = true;
        }

        hov |= ImGui.IsItemActive();

        if (hov)
        {
            Vector4 brightYellow = new(1, 1, 0.5f, 0.8f);
            new NodeBounds(this.Node).Draw(brightYellow);
            new NodeBounds(origin, this.Node).Draw(brightYellow);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("RGBA:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit4($"##{this.NodePtr:X}RGBA", ref rgba, DisplayHex))
        {
            Node->Color = new() { RGBA = RgbaVector4ToUint(rgba) };
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Multiply:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit3($"##{this.NodePtr:X}multiplyRGB", ref mult, DisplayHex))
        {
            Node->MultiplyRed = (byte)(mult.X * 255);
            Node->MultiplyGreen = (byte)(mult.Y * 255);
            Node->MultiplyBlue = (byte)(mult.Z * 255);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Add:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(124);

        if (ImGui.DragFloat3($"##{this.NodePtr:X}addRGB", ref add, 1, -255, 255, "%.0f"))
        {
            Node->AddRed = (short)add.X;
            Node->AddGreen = (short)add.Y;
            Node->AddBlue = (short)add.Z;
        }

        SplitTooltip("+/- Red", "+/- Green", "+/- Blue");

        var addTransformed = (add / 510f) + new Vector3(0.5f);

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (4 * GlobalScale));
        if (ImGui.ColorEdit3($"##{this.NodePtr:X}addRGBPicker", ref addTransformed, NoAlpha | NoInputs))
        {
            Node->AddRed = (short)Math.Floor((addTransformed.X * 510f) - 255f);
            Node->AddGreen = (short)Math.Floor((addTransformed.Y * 510f) - 255f);
            Node->AddBlue = (short)Math.Floor((addTransformed.Z * 510f) - 255f);
        }
    }
}

internal unsafe partial class CounterNodeTree
{
    /// <inheritdoc/>
    protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var str = CntNode->NodeText.ToString();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Counter:");
        ImGui.TableNextColumn();

        ImGui.SetNextItemWidth(150);
        if (ImGui.InputText($"##{this.NodePtr:X}counterEdit", ref str, 512, EnterReturnsTrue))
        {
            CntNode->SetText(str);
        }
    }
}

internal unsafe partial class ImageNodeTree
{
    internal static int TexDisplayStyle { get; set; }

    /// <inheritdoc/>
    protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var partId = (int)this.PartId;
        var partcount = ImgNode->PartsList->PartCount;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Part Id:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"##partId{this.NodePtr:X}", ref partId, 1, 1))
        {
            if (partId < 0)
            {
                partId = 0;
            }

            if (partId >= partcount)
            {
                partId = (int)(partcount - 1);
            }

            ImgNode->PartId = (ushort)partId;
        }
    }
}

internal unsafe partial class NineGridNodeTree
{
    /// <inheritdoc/>
    protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var lr = new Vector2(this.Offsets.Left, this.Offsets.Right);
        var tb = new Vector2(this.Offsets.Top, this.Offsets.Bottom);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Ninegrid Offsets:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{this.NodePtr:X}ngOffsetLR", ref lr, 1, 0))
        {
            NgNode->LeftOffset = (short)Math.Max(0, lr.X);
            NgNode->RightOffset = (short)Math.Max(0, lr.Y);
        }

        SplitTooltip("Left", "Right");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.DragFloat2($"##{this.NodePtr:X}ngOffsetTB", ref tb, 1, 0))
        {
            NgNode->TopOffset = (short)Math.Max(0, tb.X);
            NgNode->BottomOffset = (short)Math.Max(0, tb.Y);
        }

        SplitTooltip("Top", "Bottom");
    }
}

internal unsafe partial class TextNodeTree
{
    private static readonly List<FontType> FontList = Enum.GetValues<FontType>().ToList();

    private static readonly string[] FontNames = Enum.GetNames<FontType>();

    internal static bool InputAlignment(string label, ref AlignmentType alignment)
    {
        var hAlign = (int)alignment % 3;
        var vAlign = ((int)alignment - hAlign) / 3;

        var hAlignInput = IconSelectInput($"{label}H", ref hAlign, new() { 0, 1, 2 }, new() { AlignLeft, AlignCenter, AlignRight });
        var vAlignInput = IconSelectInput($"{label}V", ref vAlign, new() { 0, 1, 2 }, new() { ArrowsUpToLine, GripLines, ArrowsDownToLine });

        if (hAlignInput || vAlignInput)
        {
            alignment = (AlignmentType)((vAlign * 3) + hAlign);
            return true;
        }

        return false;
    }

    protected override void DrawEditorRows()
    {
        base.DrawEditorRows();

        var text = TxtNode->NodeText.ToString();
        var fontIndex = FontList.IndexOf(TxtNode->FontType);
        int fontSize = TxtNode->FontSize;
        var alignment = TxtNode->AlignmentType;
        var textColor = RgbaUintToVector4(TxtNode->TextColor.RGBA);
        var edgeColor = RgbaUintToVector4(TxtNode->EdgeColor.RGBA);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Text:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(Math.Max(ImGui.GetWindowContentRegionMax().X - ImGui.GetCursorPosX() - 50f, 150));
        if (ImGui.InputText($"##{this.NodePtr:X}textEdit", ref text, 512, EnterReturnsTrue))
        {
            TxtNode->SetText(text);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Font:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo($"##{this.NodePtr:X}fontType", ref fontIndex, FontNames, FontList.Count))
        {
            TxtNode->FontType = FontList[fontIndex];
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Font Size:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"##{this.NodePtr:X}fontSize", ref fontSize, 1, 10))
        {
            TxtNode->FontSize = (byte)fontSize;
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Alignment:");
        ImGui.TableNextColumn();
        if (InputAlignment($"##{this.NodePtr:X}alignment", ref alignment))
        {
            TxtNode->SetAlignment(alignment);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Text Color:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit4($"##{this.NodePtr:X}TextRGB", ref textColor, DisplayHex))
        {
            TxtNode->TextColor = new() { RGBA = RgbaVector4ToUint(textColor) };
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Edge Color:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(150);
        if (ImGui.ColorEdit4($"##{this.NodePtr:X}EdgeRGB", ref edgeColor, DisplayHex))
        {
            TxtNode->EdgeColor = new() { RGBA = RgbaVector4ToUint(edgeColor) };
        }
    }
}

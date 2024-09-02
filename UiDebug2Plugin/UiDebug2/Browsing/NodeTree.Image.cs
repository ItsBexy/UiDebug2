using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.ColorHelpers;
using static Dalamud.Utility.Util;
using static FFXIVClientStructs.FFXIV.Component.GUI.TextureType;
using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

internal unsafe partial class ImageNodeTree : ResNodeTree
{
    internal ImageNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private protected virtual uint PartId => ImgNode->PartId;

    private protected virtual AtkUldPartsList* PartsList => ImgNode->PartsList;

    private protected TextureData TexData { get; set; }

    private AtkImageNode* ImgNode => (AtkImageNode*)this.Node;

    private protected void DrawTextureAndParts()
    {
        this.TexData = new TextureData(this.PartsList, this.PartId);

        if (this.TexData.Texture == null)
        {
            return;
        }

        if (NestedTreePush($"Texture##texture{(nint)this.TexData.Texture->D3D11ShaderResourceView:X}", out _))
        {
            PrintFieldValuePairs(
                ("Texture Type", $"{this.TexData.TexType}"),
                ("Part ID", $"{this.TexData.PartId}"),
                ("Part Count", $"{this.TexData.PartCount}"));

            if (this.TexData.Path != null)
            {
                PrintFieldValuePairs(("Texture Path", this.TexData.Path));
            }

            if (ImGui.RadioButton("Full Image##textureDisplayStyle0", TexDisplayStyle == 0))
            {
                TexDisplayStyle = 0;
            }

            ImGui.SameLine();
            if (ImGui.RadioButton("Parts List##textureDisplayStyle1", TexDisplayStyle == 1))
            {
                TexDisplayStyle = 1;
            }

            ImGui.NewLine();

            if (TexDisplayStyle == 1)
            {
                this.PrintPartsTable();
            }
            else
            {
                this.DrawFullTexture();
            }

            ImGui.TreePop();
        }
    }

    private protected virtual void DrawPartOutline(uint partId, Vector2 originPos, Vector2 imagePos, Vector4 col, bool reqHover = false)
    {
        var part = this.TexData.PartsList->Parts[partId];

        var hrFactor = this.TexData.HiRes ? 2f : 1f;

        var uv = new Vector2(part.U, part.V) * hrFactor;
        var wh = new Vector2(part.Width, part.Height) * hrFactor;

        var partBegin = originPos + uv;
        var partEnd = partBegin + wh;

        if (reqHover && !ImGui.IsMouseHoveringRect(partBegin, partEnd))
        {
            return;
        }

        var savePos = ImGui.GetCursorPos();

        ImGui.GetWindowDrawList().AddRect(partBegin, partEnd, RgbaVector4ToUint(col));

        ImGui.SetCursorPos(imagePos + uv + new Vector2(0, -20));
        ImGui.TextColored(col, $"[#{partId}]\t{part.U}, {part.V}\t{part.Width}x{part.Height}");
        ImGui.SetCursorPos(savePos);
    }

    private protected override void PrintNodeObject() => ShowStruct(this.ImgNode);

    private protected override void PrintFieldsForNodeType(bool editorOpen = false)
    {
        PrintFieldValuePairs(
            ("Wrap", $"{ImgNode->WrapMode}"),
            ("Image Flags", $"0x{ImgNode->Flags:X}"));
        this.DrawTextureAndParts();
    }

    private static void PrintPartCoords(float u, float v, float w, float h, bool asFloat = false, bool lineBreak = false)
    {
        ImGui.TextDisabled($"{u}, {v},{(lineBreak ? "\n" : " ")}{w}, {h}");

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Click to copy as Vector2\nShift-click to copy as Vector4");
        }

        var suffix = asFloat ? "f" : string.Empty;

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(ImGui.IsKeyDown(ImGuiKey.ModShift)
                                       ? $"new Vector4({u}{suffix}, {v}{suffix}, {w}{suffix}, {h}{suffix})"
                                       : $"new Vector2({u}{suffix}, {v}{suffix});\nnew Vector2({w}{suffix}, {h}{suffix})");
        }
    }

    private void DrawFullTexture()
    {
        var originPos = ImGui.GetCursorScreenPos();
        var imagePos = ImGui.GetCursorPos();

        ImGui.Image(new(this.TexData.Texture->D3D11ShaderResourceView), new(this.TexData.Texture->Width, this.TexData.Texture->Height));

        for (uint p = 0; p < this.TexData.PartsList->PartCount; p++)
        {
            if (p == this.TexData.PartId)
            {
                continue;
            }

            this.DrawPartOutline(p, originPos, imagePos, new(0.6f, 0.6f, 0.6f, 1), true);
        }

        this.DrawPartOutline(this.TexData.PartId, originPos, imagePos, new(0, 0.85F, 1, 1));
    }

    private void PrintPartsTable()
    {
        ImGui.BeginTable($"partsTable##{(nint)this.TexData.Texture->D3D11ShaderResourceView:X}", 3, Borders | RowBg | Reorderable);
        ImGui.TableSetupColumn("Part ID", WidthFixed);
        ImGui.TableSetupColumn("Part Texture", WidthFixed);
        ImGui.TableSetupColumn("Coordinates", WidthFixed);

        ImGui.TableHeadersRow();

        var tWidth = this.TexData.Texture->Width;
        var tHeight = this.TexData.Texture->Height;
        var textureSize = new Vector2(tWidth, tHeight);

        for (ushort i = 0; i < this.TexData.PartCount; i++)
        {
            ImGui.TableNextColumn();

            if (i == this.TexData.PartId)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0.85F, 1, 1));
            }

            ImGui.Text($"#{i.ToString().PadLeft(this.TexData.PartCount.ToString().Length, '0')}");

            if (i == this.TexData.PartId)
            {
                ImGui.PopStyleColor(1);
            }

            ImGui.TableNextColumn();

            var part = this.TexData.PartsList->Parts[i];
            var hiRes = this.TexData.HiRes;

            var u = hiRes ? part.U * 2f : part.U;
            var v = hiRes ? part.V * 2f : part.V;
            var width = hiRes ? part.Width * 2f : part.Width;
            var height = hiRes ? part.Height * 2f : part.Height;

            ImGui.Image(new(this.TexData.Texture->D3D11ShaderResourceView), new(width, height), new Vector2(u, v) / textureSize, new Vector2(u + width, v + height) / textureSize);

            ImGui.TableNextColumn();

            ImGui.TextColored(!hiRes ? new(1) : new(0.6f, 0.6f, 0.6f, 1), "Standard:\t");
            ImGui.SameLine();
            var cursX = ImGui.GetCursorPosX();

            PrintPartCoords(u / 2f, v / 2f, width / 2f, height / 2f);

            ImGui.TextColored(hiRes ? new(1) : new(0.6f, 0.6f, 0.6f, 1), "Hi-Res:\t");
            ImGui.SameLine();
            ImGui.SetCursorPosX(cursX);

            PrintPartCoords(u, v, width, height);

            ImGui.Text("UV:\t");
            ImGui.SameLine();
            ImGui.SetCursorPosX(cursX);

            PrintPartCoords(u / tWidth, v / tWidth, (u + width) / tWidth, (v + height) / tHeight, true, true);
        }

        ImGui.EndTable();
    }

    protected struct TextureData
    {
        public AtkUldPartsList* PartsList;
        public uint PartCount;
        public uint PartId;

        public Texture* Texture = null;
        public TextureType TexType = 0;
        public string? Path = null;
        public bool HiRes = false;

        public TextureData(AtkUldPartsList* partsList, uint partId)
        {
            this.PartsList = partsList;
            this.PartCount = PartsList->PartCount;
            this.PartId = partId >= this.PartCount ? 0 : partId;

            if (this.PartsList == null)
            {
                return;
            }

            var asset = PartsList->Parts[this.PartId].UldAsset;

            if (asset == null)
            {
                return;
            }

            this.TexType = asset->AtkTexture.TextureType;

            if (this.TexType == Resource)
            {
                var resource = asset->AtkTexture.Resource;
                this.Texture = resource->KernelTextureObject;
                this.Path = Marshal.PtrToStringAnsi(new(resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr));
            }
            else
            {
                this.Texture = this.TexType == KernelTexture ? asset->AtkTexture.KernelTexture : null;
                this.Path = null;
            }

            this.HiRes = this.Path?.Contains("_hr1") ?? false;
        }
    }
}

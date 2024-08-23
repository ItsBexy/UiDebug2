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
    private AtkUldAsset* asset;

    private TextureType texType;

    private string? path;

    private Texture* texture;

    internal ImageNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
        this.asset = PartsList->Parts[this.PartId > PartsList->PartCount ? 0 : this.PartId].UldAsset;
        this.texType = asset->AtkTexture.TextureType;
        this.path = this.texType == Resource ? Marshal.PtrToStringAnsi(new(asset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.BufferPtr)) : null;
        this.texture = this.texType switch
        {
            Resource => asset->AtkTexture.Resource->KernelTextureObject,
            KernelTexture => asset->AtkTexture.KernelTexture,
            _ => null,
        };
    }

    internal AtkImageNode* ImgNode => (AtkImageNode*)this.Node;

    internal bool HiRes => this.path?.Contains("_hr1") == true;

    internal virtual uint PartId => ImgNode->PartId;

    internal virtual AtkUldPartsList* PartsList => ImgNode->PartsList;

    internal static void PrintPartCoords(float u, float v, float w, float h, bool asFloat = false, bool lineBreak = false)
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

    internal void DrawTextureAndParts()
    {
        if (this.PartsList == null || this.asset == null || this.texture == null)
        {
            return;
        }

        if (this.texture != null)
        {
            if (NestedTreePush($"Texture##texture{(nint)texture->D3D11ShaderResourceView:X}", out _))
            {
                PrintFieldValuePairs(
                    ("Texture Type", $"{this.texType}"),
                    ("Part ID", $"{this.PartId}"),
                    ("Part Count", $"{PartsList->PartCount}"));

                if (this.path != null)
                {
                    PrintFieldValuePairs(("Texture Path", this.path));
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
    }

    internal void DrawFullTexture()
    {
        var validPart = !(this.PartId > PartsList->PartCount);
        var originPos = ImGui.GetCursorScreenPos();
        var imagePos = ImGui.GetCursorPos();

        ImGui.Image(new(texture->D3D11ShaderResourceView), new(texture->Width, texture->Height));

        for (uint p = 0; p < PartsList->PartCount; p++)
        {
            if (p == this.PartId && validPart)
            {
                continue;
            }

            this.DrawPartOutline(p, originPos, imagePos, new(0.6f, 0.6f, 0.6f, 1), true);
        }

        if (validPart)
        {
            this.DrawPartOutline(this.PartId, originPos, imagePos, new(0, 0.85F, 1, 1));
        }
    }

    internal virtual void DrawPartOutline(uint partId, Vector2 originPos, Vector2 imagePos, Vector4 col, bool reqHover = false)
    {
        var savePos = ImGui.GetCursorPos();

        var part = PartsList->Parts[partId];

        var hrFactor = this.HiRes ? 2f : 1f;
        var uv = new Vector2(part.U, part.V) * hrFactor;
        var wh = new Vector2(part.Width, part.Height) * hrFactor;

        var partBegin = originPos + uv;
        var partEnd = partBegin + wh;

        if (reqHover && !ImGui.IsMouseHoveringRect(partBegin, partEnd))
        {
            return;
        }

        ImGui.GetWindowDrawList().AddRect(partBegin, partEnd, RgbaVector4ToUint(col));

        ImGui.SetCursorPos(imagePos + uv + new Vector2(0, -20));
        ImGui.TextColored(col, $"[#{partId}]\t{part.U}, {part.V}\t{part.Width}x{part.Height}");
        ImGui.SetCursorPos(savePos);
    }

    internal void PrintPartsTable()
    {
        ImGui.BeginTable($"partsTable##{(nint)texture->D3D11ShaderResourceView:X}", 3, Borders | RowBg | Reorderable);
        ImGui.TableSetupColumn("Part ID", WidthFixed);
        ImGui.TableSetupColumn("Part Texture", WidthFixed);
        ImGui.TableSetupColumn("Coordinates", WidthFixed);

        ImGui.TableHeadersRow();
        for (ushort i = 0; i < PartsList->PartCount; i++)
        {
            ImGui.TableNextColumn();

            if (i == this.PartId)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0.85F, 1, 1));
            }

            ImGui.Text($"#{i.ToString().PadLeft(PartsList->PartCount.ToString().Length, '0')}");

            if (i == this.PartId)
            {
                ImGui.PopStyleColor(1);
            }

            ImGui.TableNextColumn();

            var part = PartsList->Parts[i];
            var u = this.HiRes ? part.U * 2f : part.U;
            var v = this.HiRes ? part.V * 2f : part.V;
            var width = this.HiRes ? part.Width * 2f : part.Width;
            var height = this.HiRes ? part.Height * 2f : part.Height;
            var textureSize = new Vector2(texture->Width, texture->Height);

            ImGui.Image(new(texture->D3D11ShaderResourceView), new(width, height), new Vector2(u, v) / textureSize, new Vector2(u + width, v + height) / textureSize);

            ImGui.TableNextColumn();

            ImGui.TextColored(!this.HiRes ? new(1) : new(0.6f, 0.6f, 0.6f, 1), "Standard:\t");
            ImGui.SameLine();
            var cursX = ImGui.GetCursorPosX();

            PrintPartCoords(u / 2f, v / 2f, width / 2f, height / 2f);

            ImGui.TextColored(this.HiRes ? new(1) : new(0.6f, 0.6f, 0.6f, 1), "Hi-Res:\t");
            ImGui.SameLine();
            ImGui.SetCursorPosX(cursX);

            PrintPartCoords(u, v, width, height);

            ImGui.Text("UV:\t");
            ImGui.SameLine();
            ImGui.SetCursorPosX(cursX);

            PrintPartCoords(u / texture->Width, v / texture->Width, (u + width) / texture->Width, (v + height) / texture->Height, asFloat: true, lineBreak: true);
        }

        ImGui.EndTable();
    }

    /// <inheritdoc/>
    internal override void PrintNodeObject() => ShowStruct(this.ImgNode);

    /// <inheritdoc/>
    internal override void PrintFieldsForNodeType(bool editorOpen = false)
    {
        PrintFieldValuePairs(
            ("Wrap", $"{ImgNode->WrapMode}"),
            ("Image Flags", $"0x{ImgNode->Flags:X}"));
        this.DrawTextureAndParts();
    }
}

using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Utility.Util;

namespace UiDebug2.Browsing;

internal unsafe class ClippingMaskNodeTree : ImageNodeTree
{
    internal ClippingMaskNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    internal AtkClippingMaskNode* CmNode => (AtkClippingMaskNode*)this.Node;

    /// <inheritdoc/>
    internal override uint PartId => CmNode->PartId;

    /// <inheritdoc/>
    internal override AtkUldPartsList* PartsList => CmNode->PartsList;

    /// <inheritdoc/>
    internal override void PrintNodeObject() => ShowStruct(this.CmNode);

    /// <inheritdoc/>
    internal override void PrintFieldsForNodeType(bool editorOpen = false) => this.DrawTextureAndParts();
}

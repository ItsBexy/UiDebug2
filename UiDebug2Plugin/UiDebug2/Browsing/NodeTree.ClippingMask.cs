using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Utility.Util;

namespace UiDebug2.Browsing;

internal unsafe class ClippingMaskNodeTree : ImageNodeTree
{
    internal ClippingMaskNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private protected override uint PartId => CmNode->PartId;

    private protected override AtkUldPartsList* PartsList => CmNode->PartsList;

    private AtkClippingMaskNode* CmNode => (AtkClippingMaskNode*)this.Node;

    private protected override void PrintNodeObject() => ShowStruct(this.CmNode);

    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false) => this.DrawTextureAndParts();
}

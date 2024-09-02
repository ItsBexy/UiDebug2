using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Utility.Util;

namespace UiDebug2.Browsing;

internal unsafe class CollisionNodeTree : ResNodeTree
{
    internal CollisionNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private protected override void PrintNodeObject() => ShowStruct((AtkCollisionNode*)this.Node);
}

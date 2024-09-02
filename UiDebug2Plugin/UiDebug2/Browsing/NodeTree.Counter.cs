using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Utility.Util;
using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

internal unsafe partial class CounterNodeTree : ResNodeTree
{
    internal CounterNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private AtkCounterNode* CntNode => (AtkCounterNode*)this.Node;

    private protected override void PrintNodeObject() => ShowStruct(this.CntNode);

    private protected override void PrintFieldsForNodeType(bool editorOpen = false)
    {
        if (!editorOpen)
        {
            PrintFieldValuePairs(("Text", ((AtkCounterNode*)this.Node)->NodeText.ToString()));
        }
    }
}

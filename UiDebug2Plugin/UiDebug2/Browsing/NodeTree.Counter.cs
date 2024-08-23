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

    internal AtkCounterNode* CntNode => (AtkCounterNode*)this.Node;

    /// <inheritdoc/>
    internal override void PrintNodeObject() => ShowStruct(this.CntNode);

    /// <inheritdoc/>
    internal override void PrintFieldsForNodeType(bool editorOpen = false)
    {
        if (!editorOpen)
        {
            PrintFieldValuePairs(("Text", ((AtkCounterNode*)this.Node)->NodeText.ToString()));
        }
    }
}

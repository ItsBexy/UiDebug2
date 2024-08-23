using System.Numerics;

using FFXIVClientStructs.FFXIV.Component.GUI;

using static UiDebug2.Utility.Gui;

namespace UiDebug2.Browsing;

internal unsafe partial class NineGridNodeTree
{
    internal struct NineGridOffsets
    {
        internal int Top;
        internal int Left;
        internal int Right;
        internal int Bottom;

        internal NineGridOffsets(int top, int right, int bottom, int left)
        {
            this.Top = top;
            this.Right = right;
            this.Left = left;
            this.Bottom = bottom;
        }

        internal NineGridOffsets(Vector4 v)
            : this((int)v.X, (int)v.Y, (int)v.Z, (int)v.W)
        {
        }

        internal NineGridOffsets(AtkNineGridNode* ngNode)
            : this(ngNode->TopOffset, ngNode->RightOffset, ngNode->BottomOffset, ngNode->LeftOffset)
        {
        }

        public static implicit operator NineGridOffsets(Vector4 v) => new(v);

        public static implicit operator Vector4(NineGridOffsets v) => new(v.Top, v.Right, v.Bottom, v.Left);

        public static NineGridOffsets operator *(float n, NineGridOffsets a) => n * (Vector4)a;

        public static NineGridOffsets operator *(NineGridOffsets a, float n) => n * a;

        internal readonly void Print() => PrintFieldValuePairs(("Top", $"{this.Top}"), ("Bottom", $"{this.Bottom}"), ("Left", $"{this.Left}"), ("Right", $"{this.Right}"));
    }
}

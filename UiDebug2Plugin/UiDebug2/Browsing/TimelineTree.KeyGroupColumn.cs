using System;
using System.Collections.Generic;

using ImGuiNET;

namespace UiDebug2.Browsing;

public partial struct TimelineTree
{
    public interface IKeyGroupColumn
    {
        public string Name { get; }

        public int Count { get; }

        public float Width { get; }

        public void PrintValueAt(int i);
    }

    public struct KeyGroupColumn<T> : IKeyGroupColumn
    {
        public List<T> Values;

        public Action<T> PrintFunc;

        internal KeyGroupColumn(string name, Action<T>? printFunc = null)
        {
            this.Name = name;
            this.PrintFunc = printFunc ?? PlainTextCell;
            this.Values = new();
            this.Width = 50;
        }

        public string Name { get; set; }

        public float Width { get; init; }

        public readonly int Count => this.Values.Count;

        public static void PlainTextCell(T value) => ImGui.Text($"{value}");

        public readonly void Add(T val) => this.Values.Add(val);

        public readonly void PrintValueAt(int i)
        {
            if (this.Values.Count > i)
            {
                this.PrintFunc.Invoke(this.Values[i]);
            }
            else
            {
                ImGui.TextDisabled("...");
            }
        }
    }
}

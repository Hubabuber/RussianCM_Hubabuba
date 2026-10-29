using System;
using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client._RuCM.Kaban;

/// <summary>Measures wrapping card text at the actual column width, rather than the entire grid width.</summary>
public sealed class KabanCardGrid : Control
{
    private const float Gap = 12;
    private int _columns = 1;

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var width = float.IsFinite(availableSize.X) ? availableSize.X : 700;
        _columns = width >= 710 ? 2 : 1;
        var cellWidth = Math.Max(1, (width - Gap * (_columns - 1)) / _columns);
        var children = Children.Where(child => child.Visible).ToArray();
        var height = 0f;
        for (var index = 0; index < children.Length; index += _columns)
        {
            var rowHeight = 0f;
            for (var column = 0; column < _columns && index + column < children.Length; column++)
            {
                var child = children[index + column];
                child.Measure(new Vector2(cellWidth, float.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Y);
            }
            height += rowHeight + (index == 0 ? 0 : Gap);
        }
        return new Vector2(width, height);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var cellWidth = Math.Max(1, (finalSize.X - Gap * (_columns - 1)) / _columns);
        var children = Children.Where(child => child.Visible).ToArray();
        var y = 0f;
        for (var index = 0; index < children.Length; index += _columns)
        {
            var height = children.Skip(index).Take(_columns).Max(child => child.DesiredSize.Y);
            for (var column = 0; column < _columns && index + column < children.Length; column++)
                children[index + column].Arrange(UIBox2.FromDimensions(column * (cellWidth + Gap), y, cellWidth, height));
            y += height + Gap;
        }
        return finalSize;
    }
}

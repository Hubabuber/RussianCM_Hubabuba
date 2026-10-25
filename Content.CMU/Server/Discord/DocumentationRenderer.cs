using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Content.Server.CMU14.Discord;

/// <summary>Guidebook XML to Discord Markdown. No paraphrasing or external text generation.</summary>
public static class DocumentationRenderer
{
    public const int PageLength = 3800;

    public static string Render(string xml, Func<XElement, string> widget, Func<string, string>? keybind = null)
    {
        // The game accepts bare ampersands in document text; XML parsers do not.
        xml = Regex.Replace(xml, @"&(?!amp;|lt;|gt;|quot;|apos;|#\d+;|#x[\da-fA-F]+;)", "&amp;");
        using var reader = XmlReader.Create(new System.IO.StringReader(xml), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        });
        var root = XDocument.Load(reader, LoadOptions.PreserveWhitespace).Root
                   ?? throw new FormatException("Empty guidebook document.");
        var text = Normalize(RenderNode(root, widget));
        if (keybind != null)
            text = Regex.Replace(text, "\\[keybind=\"([^\"]*)\"\\]", m => keybind(m.Groups[1].Value));
        return text;
    }

    private static string RenderNode(XNode node, Func<XElement, string> widget)
    {
        if (node is XText text)
            return text.Value;
        if (node is not XElement element)
            return "";

        if (element.Name.LocalName == "Table")
        {
            var columns = (int?) element.Attribute("Columns") ?? 1;
            if (columns < 1 || columns > 20)
                throw new FormatException("Invalid guidebook table column count.");
            var cells = element.Elements().Select(e => Normalize(RenderNode(e, widget))).ToArray();
            var rows = new List<string>();
            // Label every cell with its column heading: readable on mobile, unlike Markdown tables.
            for (var i = columns; i < cells.Length; i += columns)
            {
                var row = new List<string>();
                for (var j = 0; j < columns && i + j < cells.Length; j++)
                    row.Add($"{cells[j]}: {cells[i + j]}");
                rows.Add("• " + string.Join("\n", row));
            }
            return "\n\n" + (rows.Count == 0 ? string.Join("\n", cells) : string.Join("\n\n", rows)) + "\n\n";
        }

        if (element.Name.LocalName is "Document" or "Box" or "ColorBox")
            return string.Concat(element.Nodes().Select(n => RenderNode(n, widget)));

        // Unknown controls fail publication instead of silently dropping documentation.
        return "\n\n" + widget(element) + "\n\n";
    }

    public static string Markdown(string text, Func<string, string?> link)
    {
        text = Regex.Replace(text, "\\[textlink=\"([^\"]*)\"\\s+link=\"([^\"]*)\"\\]", m =>
            link(m.Groups[2].Value) is { } url ? $"[{m.Groups[1].Value}]({url})" : m.Groups[1].Value);
        text = Regex.Replace(text, @"\[/?bold\]", "**");
        text = Regex.Replace(text, @"\[/?italic\]", "*");
        text = Regex.Replace(text, @"\[/?(?:color|font|size)(?:=[^\]]*)?\]", "");
        text = Regex.Replace(text, @"\[/?(?:head|bullet)\]", "");
        text = Regex.Replace(text, @"(?m)^(#{1,6})\s*(\S)", "$1 $2");
        // Discord mentions are also disabled in the payload; neutralize their visual appearance.
        return text.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
    }

    private static string Normalize(string text)
    {
        var lines = text.Replace("\r", "").Split('\n').Select(line => line.TrimEnd());
        // XML indentation is layout, but nested list indentation carries meaning.
        var normalized = lines.Select(line => Regex.IsMatch(line.TrimStart(), @"^[-•]\s")
            ? new string(' ', Math.Min(8, line.TakeWhile(char.IsWhiteSpace).Count())) + line.TrimStart()
            : line.TrimStart());
        return Regex.Replace(string.Join("\n", normalized), @"\n[ \t]*\n(?:[ \t]*\n)+", "\n\n").Trim();
    }

    /// <summary>Keep paragraphs/lines together where possible; never truncate or split a surrogate pair.</summary>
    public static List<string> Pages(string text, int limit = PageLength)
    {
        if (limit < 4)
            throw new ArgumentOutOfRangeException(nameof(limit));
        var pages = new List<string>();
        while (text.Length > limit)
        {
            var split = text.LastIndexOf("\n\n", limit, StringComparison.Ordinal);
            if (split < limit / 2)
                split = text.LastIndexOf('\n', limit);
            if (split < limit / 2)
                split = text.LastIndexOf(' ', limit);
            if (split < limit / 2)
                split = limit;
            if (char.IsHighSurrogate(text[split - 1]))
                split--;
            pages.Add(text[..split]);
            text = text[split..];
            // Preserve all non-whitespace text, including the start of the next sentence.
            text = text.TrimStart('\r', '\n', ' ');
        }
        if (text.Length > 0)
            pages.Add(text);
        return pages;
    }
}

public sealed record DocumentationPage(string Id, string Title, string Text, string Source, string Category, int Color);

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Guidebook;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;

namespace Content.Server.CMU14.Discord;

/// <summary>Captures game resources on the game thread, publishes immutable snapshots in the background.</summary>
public sealed partial class CMUDiscordDocumentationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ILocalizationManager _localization = default!;

    private readonly CancellationTokenSource _shutdown = new();
    private Task<string>? _publication;
    private CancellationTokenSource? _publicationCancel;
    private DateTime _next = DateTime.UtcNow.AddSeconds(30);
    private string _rulesWebhook = "";
    private string _guidesWebhook = "";
    private int _interval;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_publication is { IsCompleted: true })
        {
            Log.Info(_publication.GetAwaiter().GetResult());
            _publication = null;
            var cancelled = _publicationCancel?.IsCancellationRequested == true;
            _publicationCancel?.Dispose();
            _publicationCancel = null;
            _next = DateTime.UtcNow.AddSeconds(cancelled ? 30 : Math.Max(60, _config.GetCVar(CCVars.CMUDiscordDocumentationInterval)));
        }

        var rulesWebhook = _config.GetCVar(CCVars.CMUDiscordRulesWebhook).Trim();
        var guidesWebhook = _config.GetCVar(CCVars.CMUDiscordGuidesWebhook).Trim();
        var interval = _config.GetCVar(CCVars.CMUDiscordDocumentationInterval);
        // Detect changes without logging or exposing confidential configuration.
        if (_rulesWebhook != rulesWebhook || _guidesWebhook != guidesWebhook || _interval != interval)
        {
            _rulesWebhook = rulesWebhook;
            _guidesWebhook = guidesWebhook;
            _interval = interval;
            _next = DateTime.UtcNow.AddSeconds(30);
            _publicationCancel?.Cancel();
        }
        if (_publication != null)
            return;
        if (interval <= 0 || rulesWebhook.Length == 0 && guidesWebhook.Length == 0 || DateTime.UtcNow < _next)
            return;
        _next = DateTime.UtcNow.AddSeconds(Math.Max(60, interval));

        try
        {
            var directory = Path.Combine(_resources.UserData.RootDir
                                         ?? throw new InvalidOperationException("Server user-data directory is unavailable."),
                "cmu-discord-documentation");
            var rules = CaptureSafely(rulesWebhook, true);
            var guides = CaptureSafely(guidesWebhook, false);
            if (rules == null && guides == null)
                return;
            var presentation = new DocumentationPresentation(Text("username"), Text("rules-title"), Text("guides-title"),
                Text("rules-intro"), Text("guides-intro"), Text("contents"), Text("source-label"));
            _publicationCancel = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            var cancel = _publicationCancel.Token;
            // Never access prototypes, localization, entities or game state inside this task.
            _publication = Task.Run(async () =>
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    // Prevent two processes sharing this user-data directory from publishing concurrently.
                    using var ownership = new FileStream(Path.Combine(directory, "publisher.lock"),
                        FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
                    {
                        Timeout = TimeSpan.FromSeconds(30),
                    };
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("RuCMU-Documentation/1.0");
                    var publisher = new DocumentationPublisher(http, new DocumentationFileStore(directory), presentation);
                    var results = new List<string>();
                    // One unavailable channel must not prevent the other from updating.
                    foreach (var (webhook, pages, isRules) in new[]
                             {
                                 (rulesWebhook, rules, true), (guidesWebhook, guides, false),
                             })
                    {
                        if (webhook.Length == 0 || pages == null)
                            continue;
                        var name = isRules ? "rules" : "guides";
                        try
                        {
                            await publisher.Synchronize(webhook, pages, isRules, cancel);
                            results.Add($"{name}: synchronized {pages.Count} documents");
                        }
                        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                        {
                            return "Discord documentation publication cancelled.";
                        }
                        catch (Exception e)
                        {
                            // Exception messages can contain webhook tokens (e.g. HttpRequestException).
                            var reason = e is DocumentationHttpException error ? $"HTTP {error.Status}" : e.GetType().Name;
                            results.Add($"{name}: stopped ({reason}); check configuration and publication journal");
                        }
                    }
                    return "Discord documentation: " + string.Join("; ", results);
                }
                catch (Exception e)
                {
                    return $"Discord documentation stopped ({e.GetType().Name}); check user-data storage.";
                }
            });
        }
        catch (Exception e)
        {
            Log.Error($"Discord documentation snapshot failed ({e.GetType().Name}); no messages were changed.");
        }
    }

    private List<DocumentationPage>? CaptureSafely(string webhook, bool rules)
    {
        if (webhook.Length == 0)
            return null;
        try
        {
            return rules ? CaptureRules() : CaptureGuides();
        }
        catch (Exception e)
        {
            Log.Error($"Discord documentation {(rules ? "rules" : "guides")} snapshot failed ({e.GetType().Name}); this channel was left unchanged.");
            return null;
        }
    }

    public override void Shutdown()
    {
        _shutdown.Cancel();
        base.Shutdown();
    }

    private List<DocumentationPage> CaptureRules()
    {
        var entries = new List<GuideEntryPrototype>();
        Visit("RMCOverview", entries, new HashSet<string>());
        return Capture(entries);
    }

    private void Visit(string id, List<GuideEntryPrototype> entries, HashSet<string> visited)
    {
        if (!visited.Add(id))
            return;
        var entry = _prototypes.Index<GuideEntryPrototype>(id);
        // Only this project's rules/CMU guides, never generic SS14 or unrelated RMC documentation.
        if (!entry.Text.ToString().StartsWith("/ServerInfo/Guidebook/CMU14/", StringComparison.Ordinal) && id != "RMCOverview")
            return;
        entries.Add(entry);
        foreach (var child in entry.Children)
            Visit(child.Id, entries, visited);
    }

    private List<DocumentationPage> CaptureGuides()
    {
        var entries = new List<GuideEntryPrototype>();
        var visited = new HashSet<string>();
        foreach (var root in new[] { "CMUGuidebook", "AU14SOP", "AU14UCMJ", "AU14CCLaw" })
            Visit(root, entries, visited);
        // Include active CMU entries even if a new section is not yet linked from the root.
        foreach (var entry in _prototypes.EnumeratePrototypes<GuideEntryPrototype>().OrderBy(p => p.ID, StringComparer.Ordinal))
        {
            var path = entry.Text.ToString();
            if (path.StartsWith("/ServerInfo/Guidebook/CMU14/", StringComparison.Ordinal) &&
                !path.StartsWith("/ServerInfo/Guidebook/CMU14/Rules/", StringComparison.Ordinal))
                Visit(entry.ID, entries, visited);
        }
        return Capture(entries);
    }

    private List<DocumentationPage> Capture(List<GuideEntryPrototype> entries)
    {
        var pages = new List<DocumentationPage>();
        var widgets = new Dictionary<string, string>();
        var bindings = DefaultBindings();
        foreach (var entry in entries)
        {
            var path = entry.Text.ToString();
            var category = Category(path);
            var title = _localization.TryGetString(entry.Name, out var translated) ? translated : entry.Name;
            var text = DocumentationRenderer.Render(_resources.ContentFileReadAllText(entry.Text), element =>
            {
                var key = element.ToString();
                if (!widgets.TryGetValue(key, out var rendered))
                    widgets[key] = rendered = RenderWidget(element);
                return rendered;
            }, id => bindings.TryGetValue(id, out var binding) ? binding : Text("keybind-missing"));
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException($"Empty guidebook entry {entry.ID}.");
            pages.Add(new DocumentationPage(entry.ID, title, text, Source(path), category.Name, category.Color));
        }
        return pages;
    }

    private Dictionary<string, string> DefaultBindings()
    {
        var yaml = _resources.ContentFileReadYaml(new Robust.Shared.Utility.ResPath("/keybinds.yml"));
        var mapping = (YamlMappingNode) yaml.Documents[0].RootNode;
        var bindings = (YamlSequenceNode) mapping.Children[new YamlScalarNode("binds")];
        var result = new Dictionary<string, string>();
        foreach (YamlMappingNode binding in bindings)
        {
            string? Value(string key) => binding.Children.TryGetValue(new YamlScalarNode(key), out var value)
                ? ((YamlScalarNode) value).Value : null;
            if (Value("function") is not { } function || Value("key") is not { } key)
                continue;
            var keys = new[] { Value("mod1"), Value("mod2"), Value("mod3"), key }.Where(p => p != null);
            var name = Regex.Replace(function, "([A-Z]+)([A-Z][a-z])", "$1-$2");
            name = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
            var label = _localization.TryGetString("ui-options-function-" + name, out var localized)
                ? localized : Text("keybind-action");
            result.TryAdd(function, Text("keybind", ("action", label), ("keys", string.Join("+", keys))));
        }
        return result;
    }

    private string Text(string key, params (string, object)[] args) =>
        _localization.GetString("cmu-discord-docs-" + key, args);

    private (string Name, int Color) Category(string path)
    {
        if (path.Contains("/Rules/") || path.EndsWith("/RMCOverview.xml"))
            return (Text("category-rules"), 0xD9A441);
        if (path.Contains("/SOP/"))
            return (Text("category-sop"), 0x578A60);
        if (path.Contains("/UCMJ/"))
            return (Text("category-ucmj"), 0xAD6654);
        if (path.Contains("/CCLaw/"))
            return (Text("category-cclaw"), 0xB79454);
        if (path.Contains("/Lore/"))
            return (Text("category-lore"), 0x9176BC);
        if (path.Contains("/Chemicals/"))
            return (Text("category-chemistry"), 0x5CAB9A);
        return (Text("category-guides"), 0x419CB8);
    }

    private static string Source(string path)
    {
        var folder = path.StartsWith("/ServerInfo/Guidebook/CMU14/", StringComparison.Ordinal) ||
                     path.StartsWith("/Textures/CMU14/", StringComparison.Ordinal)
            ? "Content.CMU/Resources" : "Resources";
        return "https://github.com/flex5hybrid/RussianCM/blob/master/" + folder + path;
    }
}

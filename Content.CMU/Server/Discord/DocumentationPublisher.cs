using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Discord;

namespace Content.Server.CMU14.Discord;

/// <summary>Serial, checkpointed reconciliation of messages owned by this publisher.</summary>
public sealed class DocumentationPublisher(HttpClient http, IDocumentationStore store, DocumentationPresentation presentation)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task Synchronize(string webhook, IReadOnlyList<DocumentationPage> documents, bool rules,
        CancellationToken cancel)
    {
        if (documents.Count == 0)
            throw new InvalidOperationException("Documentation selection is empty; publication stopped.");
        var endpoint = ParseWebhook(webhook);
        using var metadataResponse = await Send(HttpMethod.Get, endpoint.Url, null, cancel);
        using var metadata = JsonDocument.Parse(await metadataResponse.Content.ReadAsStringAsync(cancel));
        var channel = metadata.RootElement.GetProperty("channel_id").GetString()!;
        var guild = metadata.RootElement.GetProperty("guild_id").GetString()!;
        var destination = $"{endpoint.Id}:{channel}:{endpoint.Thread}";
        var state = store.Load(destination);
        var prefix = rules ? "rules/" : "guides/";
        var messageBase = $"https://discord.com/channels/{guild}/{endpoint.Thread ?? channel}/";
        var verified = new HashSet<string>();
        var documentIds = documents.Select(p => p.Id).ToHashSet();

        string? Link(string id) => documentIds.Contains(id) && state.Messages.TryGetValue(prefix + id + "/0", out var message)
                                  && message.Id != null ? messageBase + message.Id : null;

        // Reserve the index at the top before publishing the documents. A second pass installs live links.
        for (var pass = 0; pass < 2; pass++)
        {
            var desired = BuildMessages(documents, rules, Link, presentation);
            foreach (var (key, payload) in desired)
                await Upsert(prefix + key, payload, state, destination, endpoint, verified, cancel);

            if (pass == 0)
                continue;

            var keep = desired.Select(p => prefix + p.Key).ToHashSet();
            foreach (var key in state.Messages.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal) && !keep.Contains(k)).ToArray())
            {
                var old = state.Messages[key];
                if (old.Pending)
                    throw new InvalidOperationException("Unconfirmed Discord message creation; reconcile the publication journal first.");
                if (old.Id != null)
                {
                    using var response = await Send(HttpMethod.Delete, endpoint.Message(old.Id), null, cancel, allowMissing: true);
                }
                state.Messages.Remove(key);
                store.Save(destination, state);
            }
        }
    }

    public static List<KeyValuePair<string, WebhookPayload>> BuildMessages(IReadOnlyList<DocumentationPage> documents,
        bool rules, Func<string, string?> link, DocumentationPresentation presentation)
    {
        var title = rules ? presentation.RulesTitle : presentation.GuidesTitle;
        var intro = rules ? presentation.RulesIntro : presentation.GuidesIntro;
        var index = new StringBuilder(intro + "\n\n");
        string? category = null;
        foreach (var document in documents)
        {
            if (category != document.Category)
            {
                category = document.Category;
                index.AppendLine($"\n**{category}**");
            }
            var url = link(document.Id) ?? document.Source;
            index.AppendLine($"• [{document.Title}]({url})");
        }

        var result = new List<KeyValuePair<string, WebhookPayload>>();
        var contents = DocumentationRenderer.Pages(index.ToString());
        for (var i = 0; i < contents.Count; i++)
            result.Add(new($"@index/{i}", Payload(title, contents[i], presentation.Contents, i, contents.Count,
                rules ? 0xD9A441 : 0x419CB8, null, presentation)));

        foreach (var document in documents)
        {
            var pages = DocumentationRenderer.Pages(DocumentationRenderer.Markdown(document.Text,
                id => link(id) ?? documents.FirstOrDefault(p => p.Id == id)?.Source));
            for (var i = 0; i < pages.Count; i++)
                result.Add(new($"{document.Id}/{i}", Payload(document.Title, pages[i], document.Category, i,
                    pages.Count, document.Color, document.Source, presentation)));
        }
        return result;
    }

    private static WebhookPayload Payload(string title, string description, string category, int page, int total,
        int color, string? source, DocumentationPresentation presentation)
    {
        var suffix = total > 1 ? $" • {page + 1}/{total}" : "";
        return new WebhookPayload
        {
            Username = presentation.Username,
            Content = source == null ? "" : $"-# [{presentation.SourceLabel}]({source})",
            Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Title = title[..Math.Min(title.Length, 256 - suffix.Length)] + suffix,
                    Description = description,
                    Color = color,
                    Footer = new WebhookEmbedFooter { Text = $"RuCMU • {category}{suffix}" },
                    Fields = new List<WebhookEmbedField>(),
                },
            },
            AllowedMentions = new WebhookMentions(),
        };
    }

    private async Task Upsert(string key, WebhookPayload payload, DocumentationJournal state, string destination,
        DocumentationEndpoint endpoint, HashSet<string> verified, CancellationToken cancel)
    {
        var body = JsonSerializer.Serialize(payload, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        if (!state.Messages.TryGetValue(key, out var entry))
            state.Messages[key] = entry = new DocumentationMessage();
        if (entry.Pending)
            throw new InvalidOperationException("Unconfirmed Discord message creation; reconcile the publication journal first.");

        if (entry.Id != null && !verified.Contains(key))
        {
            using var existing = await Send(HttpMethod.Get, endpoint.Message(entry.Id), null, cancel, allowMissing: true);
            if (existing.StatusCode == HttpStatusCode.NotFound)
            {
                entry.Id = null;
                entry.Hash = "";
                store.Save(destination, state);
            }
            else
                verified.Add(key);
        }
        if (entry.Id != null && entry.Hash == hash)
            return;

        if (entry.Id == null)
        {
            // A crash or ambiguous network failure must not cause duplicate POSTs on the next run.
            entry.Pending = true;
            store.Save(destination, state);
            HttpResponseMessage created;
            try
            {
                created = await Send(HttpMethod.Post, endpoint.Url + "?wait=true" + endpoint.ThreadQuery('&'), body, cancel);
            }
            catch (DocumentationHttpException e) when (e.Status is >= 400 and < 500 && e.Status != 408)
            {
                // A definite rejection did not create a message. A corrected configuration can retry.
                entry.Pending = false;
                store.Save(destination, state);
                throw;
            }
            using var response = created;
            using var message = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancel));
            entry.Id = message.RootElement.GetProperty("id").GetString()
                       ?? throw new InvalidOperationException("Discord returned no message ID.");
            entry.Pending = false;
        }
        else
        {
            using var response = await Send(HttpMethod.Patch, endpoint.Message(entry.Id), body, cancel);
        }
        entry.Hash = hash;
        store.Save(destination, state);
        verified.Add(key);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string url, string? body,
        CancellationToken cancel, bool allowMissing = false)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            if (body != null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await http.SendAsync(request, cancel);
            var retry = response.StatusCode == HttpStatusCode.TooManyRequests ||
                        (method != HttpMethod.Post && (int) response.StatusCode >= 500);
            if (retry && attempt < 5)
            {
                var retryAfter = response.Headers.RetryAfter;
                var delay = retryAfter?.Delta?.TotalSeconds ??
                            (retryAfter?.Date - DateTimeOffset.UtcNow)?.TotalSeconds ?? Math.Pow(2, attempt + 1);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancel));
                    if (json.RootElement.TryGetProperty("retry_after", out var after))
                        delay = Math.Max(delay, after.GetDouble());
                }
                response.Dispose();
                if (delay > 300)
                    throw new InvalidOperationException("Discord requested an extended cooldown; publication stopped.");
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, delay)), cancel);
                continue;
            }
            if (response.IsSuccessStatusCode || allowMissing && response.StatusCode == HttpStatusCode.NotFound)
                return response;
            var status = (int) response.StatusCode;
            response.Dispose();
            // Never include URLs, response bodies or HttpRequestException messages in server logs.
            throw new DocumentationHttpException(status);
        }
    }

    public static DocumentationEndpoint ParseWebhook(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.Host is not ("discord.com" or "discordapp.com") || !uri.IsDefaultPort ||
            uri.UserInfo.Length != 0 || uri.Fragment.Length != 0)
            throw new FormatException("Expected a Discord webhook URL.");
        var match = Regex.Match(uri.AbsolutePath, @"^/api(?:/v\d+)?/webhooks/(\d+)/([\w.-]+)/?$");
        if (!match.Success)
            throw new FormatException("Expected a Discord webhook URL.");
        string? thread = null;
        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            if (pair.Length != 2 || pair[0] != "thread_id" || !Regex.IsMatch(pair[1], @"^\d+$") || thread != null)
                throw new FormatException("Only a numeric thread_id is supported in webhook URLs.");
            thread = pair[1];
        }
        return new DocumentationEndpoint(match.Groups[1].Value,
            $"https://discord.com/api/v10/webhooks/{match.Groups[1].Value}/{match.Groups[2].Value}", thread);
    }
}

/// <summary>Localized on the game thread; background publication never touches the localization manager.</summary>
public sealed record DocumentationPresentation(string Username, string RulesTitle, string GuidesTitle,
    string RulesIntro, string GuidesIntro, string Contents, string SourceLabel);

public sealed class DocumentationHttpException(int status) : InvalidOperationException($"Discord documentation HTTP {status}.")
{
    public int Status { get; } = status;
}

public sealed record DocumentationEndpoint(string Id, string Url, string? Thread)
{
    public string ThreadQuery(char separator) => Thread == null ? "" : $"{separator}thread_id={Thread}";
    public string Message(string id) => $"{Url}/messages/{id}" + ThreadQuery('?');
    public override string ToString() => $"Discord webhook {Id}";
}

public sealed class DocumentationMessage
{
    public string? Id { get; set; }
    public string Hash { get; set; } = "";
    public bool Pending { get; set; }
}

public sealed class DocumentationJournal
{
    public int Version { get; set; } = 1;
    public string Destination { get; set; } = "";
    public Dictionary<string, DocumentationMessage> Messages { get; set; } = new();
}

public interface IDocumentationStore
{
    DocumentationJournal Load(string destination);
    void Save(string destination, DocumentationJournal journal);
}

public sealed class DocumentationFileStore(string directory) : IDocumentationStore
{
    private string PathFor(string destination) => Path.Combine(directory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(destination))) + ".json");

    public DocumentationJournal Load(string destination)
    {
        var path = PathFor(destination);
        if (!File.Exists(path))
            return new DocumentationJournal { Destination = destination };
        var state = JsonSerializer.Deserialize<DocumentationJournal>(File.ReadAllText(path))
                    ?? throw new InvalidDataException("Invalid documentation journal.");
        if (state.Version != 1 || state.Messages == null || state.Destination != destination)
            throw new InvalidDataException("Unsupported documentation journal.");
        return state;
    }

    public void Save(string destination, DocumentationJournal journal)
    {
        Directory.CreateDirectory(directory);
        var path = PathFor(destination);
        using (var file = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(file, journal);
            file.Flush(true);
        }
        File.Move(path + ".tmp", path, true);
    }
}

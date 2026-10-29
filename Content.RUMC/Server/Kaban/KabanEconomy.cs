using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._RuCM.Kaban;
using Microsoft.EntityFrameworkCore;

namespace Content.Server._RuCM.Kaban;

// CMU14: policy is built from the authenticated session and server configuration, never from the request.
public sealed record KabanPolicy(Guid Actor, bool Manager, bool Senior, bool Shop, bool Priorities,
    bool Tts, bool Discord, string Server,
    string Round, bool Lobby, string[] CommandJobs, string[] AvailableJobs,
    bool VoiceAvailable = true, bool VoiceVerified = true, bool Forcing = false,
    KabanForceOption[]? ForceOptions = null);

public sealed class KabanDenied(string key) : Exception(key);

/// <summary>
/// All writers acquire the same database row before reading. This serializes economic decisions
/// across processes, including direct balance changes and rolling limits. The lock and all changes commit together.
/// Calls run through ServerDbManager's worker pool; no database work is done in a job lottery.
/// </summary>
public static partial class KabanEconomy
{
    // JSON is server-only. Keep the original request fingerprints when TargetName is absent,
    // without putting JSON attributes/types into the sandboxed network contract.
    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                info =>
                {
                    if (info.Type == typeof(KabanRequest))
                        foreach (var property in info.Properties)
                            if (property.Name == nameof(KabanRequest.TargetName))
                                property.ShouldSerialize = (_, value) => value != null;
                },
            },
        },
    };

    public static bool IsPriority(string id)
        => id is "priority3" or "priority5" or "pack3" or "pack5" or "flex3" or "flex5";

    public static bool IsForcing(string id)
        => id is "mode" or "map" or "govfor" or "opfor" or "full";

    public static string Blocker(string id, KabanPolicy policy)
    {
        if (!policy.Shop)
            return "cmu-kaban-shop-disabled";
        if (IsForcing(id) && !policy.Forcing)
            return "cmu-kaban-forcing-disabled";
        if (IsPriority(id) && !policy.Priorities)
            return "cmu-kaban-priorities-disabled";
        if (id == "voice" && !policy.Tts)
            return "cmu-kaban-tts-disabled";
        if (id is "discord" or "rename" && !policy.Discord)
            return "cmu-kaban-discord-disabled";
        return "";
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string key = "cmu-kaban-invalid")
    {
        if (!condition)
            throw new KabanDenied(key);
    }

    public static async Task<string> Execute(ServerDbContext db, KabanPolicy policy, KabanRequest request, DateTime utcNow)
    {
        Require(request.Operation != Guid.Empty && policy.Actor != Guid.Empty);
        Require(request.Text.Length <= 2000 && request.Product.Length <= 32 && request.Category.Length <= 32 && (request.TargetName?.Length ?? 0) <= 64
            && request.Jobs.Length <= 3 && request.Jobs.All(j => j.Length <= 128));
        var requestJson = JsonSerializer.Serialize(request, RequestJsonOptions);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        await using var tx = await db.Database.BeginTransactionAsync();
        Require(await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1") == 1,
            "cmu-kaban-storage-unavailable");
        var previous = await db.Set<KabanOperation>().SingleOrDefaultAsync(x => x.Id == request.Operation);
        if (previous != null)
        {
            Require(previous.Actor == policy.Actor && previous.Fingerprint == fingerprint, "cmu-kaban-replay-mismatch");
            return previous.Result;
        }

        var now = utcNow.Ticks;
        var target = policy.Actor;
        switch (request.Action)
        {
            case KabanAction.Purchase:
                Require(request.Confirmed, "cmu-kaban-confirm-required");
                await Purchase(db, policy, request, now);
                break;
            case KabanAction.Nominate:
            case KabanAction.Approve:
            case KabanAction.Reject:
                // Preserve wire values, but retire the former nomination/approval workflow.
                throw new KabanDenied("cmu-kaban-forbidden");
            case KabanAction.Arm:
                await Arm(db, policy, request, now);
                break;
            case KabanAction.Disarm:
            {
                Require(policy.Lobby, "cmu-kaban-lobby-only");
                var token = await db.Set<KabanToken>().SingleOrDefaultAsync(t => t.Id == request.Item && t.UserId == policy.Actor);
                Require(token != null && token.Status == "reserved" && token.Server == policy.Server);
                var use = await db.Set<KabanTokenUse>().SingleAsync(u => u.Id == token!.Reservation);
                Require(use.Status == "reserved");
                use.Status = "refunded";
                token.Status = "available";
                token.Reservation = Guid.Empty;
                break;
            }
            case KabanAction.CancelRequest:
            {
                var queued = await db.Set<KabanRoundRequest>().SingleOrDefaultAsync(q => q.Id == request.Item);
                Require(queued != null && (queued.UserId == policy.Actor || policy.Manager), "cmu-kaban-forbidden");
                Require(queued!.Status == "queued");
                queued.Status = "refunded";
                target = queued.UserId;
                await ChangeBalance(db, target, policy.Actor, queued.Price, request.Operation, now, "force-refund");
                break;
            }
            case KabanAction.ApproveVoice:
            {
                Require(policy.Manager && policy.Tts, "cmu-kaban-forbidden");
                Require(policy.VoiceVerified, "cmu-kaban-voice-unavailable");
                var voice = await db.Set<KabanVoice>().SingleOrDefaultAsync(v => v.UserId == request.Target);
                Require(voice != null && voice.Status == "pending");
                // Catalog membership and technical availability are checked by the TTS owner before this command.
                Require(request.Text == voice!.Speaker && voice.Consent.Trim().Length >= 20);
                voice.Status = "active";
                target = voice.UserId;
                break;
            }
            case KabanAction.GrantVoice:
                Require(policy.Senior && policy.Tts && request.Confirmed && request.Target != Guid.Empty
                    && request.Target != policy.Actor, "cmu-kaban-forbidden");
                Require(policy.VoiceAvailable, "cmu-kaban-voice-exists");
                Require(await db.Player.AnyAsync(p => p.UserId == request.Target), "cmu-kaban-unknown-account");
                db.Add(new KabanPurchase { Id = request.Operation, UserId = request.Target,
                    ProductId = "voice", Price = 0, At = now });
                await PrepareVoice(db, request.Target, request.Operation, request.Category, request.Text);
                await ChangeBalance(db, request.Target, policy.Actor, 0, request.Operation, now, "voice-special-grant");
                target = request.Target;
                break;
            case KabanAction.ApproveDiscord:
            {
                Require(policy.Manager && policy.Discord, "cmu-kaban-forbidden");
                var role = await db.Set<KabanDiscordRole>().SingleOrDefaultAsync(v => v.UserId == request.Target);
                Require(role != null && role.Status == "pending");
                role!.Status = "approved";
                target = role.UserId;
                break;
            }
            case KabanAction.RejectPrivilege:
            {
                Require(policy.Manager, "cmu-kaban-forbidden");
                Guid purchaseId;
                if (request.Product == "voice")
                {
                    var voice = await db.Set<KabanVoice>().SingleAsync(v => v.UserId == request.Target);
                    Require(voice.Status == "pending");
                    voice.Status = "rejected";
                    purchaseId = voice.PurchaseId;
                }
                else
                {
                    var role = await db.Set<KabanDiscordRole>().SingleAsync(v => v.UserId == request.Target);
                    Require(role.Status == "pending");
                    role.Status = role.RoleId.Length > 0 ? "active" : "rejected";
                    role.Name = role.PreviousName;
                    role.Color = role.PreviousColor;
                    purchaseId = role.PurchaseId;
                }
                var purchase = await db.Set<KabanPurchase>().SingleAsync(p => p.Id == purchaseId);
                target = request.Target;
                await ChangeBalance(db, target, policy.Actor, purchase.Price, request.Operation, now, "privilege-refund");
                break;
            }
            case KabanAction.SetProduct:
            {
                Require(policy.Senior && request.Amount > 0 && request.Amount <= 100000 && request.Confirmed,
                    "cmu-kaban-forbidden");
                var product = await db.Set<KabanProduct>().SingleAsync(p => p.Id == request.Product);
                product.Price = request.Amount;
                product.Enabled = !request.Exceptional;
                break;
            }
            case KabanAction.CorrectBalance:
                Require(policy.Manager, "cmu-kaban-forbidden");
                Require(request.Confirmed, "cmu-kaban-confirm-required");
                Require(request.Target == Guid.Empty && !string.IsNullOrWhiteSpace(request.TargetName) && request.Amount != 0);
                var name = request.TargetName!.Trim().ToUpperInvariant();
                var matches = await db.Player.Where(p => p.LastSeenUserName.ToUpper() == name)
                    .Select(p => p.UserId).Take(2).ToArrayAsync();
                Require(matches.Length > 0, "cmu-kaban-unknown-account");
                Require(matches.Length == 1, "cmu-kaban-ambiguous-account");
                target = matches[0];
                await ChangeBalance(db, target, policy.Actor, request.Amount, request.Operation, now,
                    "manager-adjustment:" + request.Text.Trim(), earned: request.Amount > 0);
                break;
            default:
                throw new KabanDenied("cmu-kaban-invalid");
        }
        db.Add(new KabanOperation
        {
            Id = request.Operation, Actor = policy.Actor, Fingerprint = fingerprint, Result = "cmu-kaban-ok", At = now,
        });
        db.Add(new KabanAudit
        {
            Id = Guid.NewGuid(), Operation = request.Operation, Actor = policy.Actor, Target = target,
            Action = request.Action.ToString(), Detail = requestJson, At = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return "cmu-kaban-ok";
    }

    private static async Task ChangeBalance(ServerDbContext db, Guid user, Guid actor, long delta,
        Guid operation, long now, string reason, bool earned = false)
    {
        var wallet = await db.Set<KabanWallet>().SingleOrDefaultAsync(w => w.UserId == user);
        if (wallet == null)
        {
            wallet = new KabanWallet { UserId = user };
            db.Add(wallet);
        }
        Require(delta >= -wallet.Balance, "cmu-kaban-insufficient");
        wallet.Balance = checked(wallet.Balance + delta);
        if (earned)
            wallet.Earned = checked(wallet.Earned + delta);
        db.Add(new KabanLedger
        {
            Id = operation, UserId = user, Actor = actor, Delta = delta,
            Balance = wallet.Balance, At = now, Reason = reason,
        });
    }

    private static async Task Purchase(ServerDbContext db, KabanPolicy policy, KabanRequest request, long now)
    {
        var product = await db.Set<KabanProduct>().SingleOrDefaultAsync(p => p.Id == request.Product);
        Require(product != null && product.Enabled, "cmu-kaban-product-unavailable");
        var blocker = Blocker(request.Product, policy);
        Require(blocker.Length == 0, blocker);
        // Displayed prices are bound to confirmation, so an admin price change cannot silently charge more.
        Require(request.Amount == product!.Price, "cmu-kaban-price-changed");
        var purchase = new KabanPurchase
        {
            Id = request.Operation, UserId = policy.Actor, ProductId = product.Id, Price = product.Price, At = now,
        };
        if (IsPriority(product.Id))
        {
            Require(await db.Set<KabanToken>().CountAsync(t => t.UserId == policy.Actor && t.Status != "spent" && t.Expires > now) < 30,
                "cmu-kaban-inventory-limit");
            var pack = product.Id.StartsWith("pack", StringComparison.Ordinal);
            for (var i = 0; i < (pack ? 3 : 1); i++)
                db.Add(new KabanToken
                {
                    Id = Guid.NewGuid(), UserId = policy.Actor, PurchaseId = purchase.Id,
                    Multiplier = product.Id.EndsWith('5') ? 5 : 3,
                    Flexible = product.Id.StartsWith("flex", StringComparison.Ordinal),
                    Expires = checked(now + TimeSpan.FromDays(pack ? 45 : 30).Ticks),
                });
        }
        else if (product.Id == "voice")
        {
            Require(policy.VoiceAvailable, "cmu-kaban-voice-exists");
            await PrepareVoice(db, policy.Actor, purchase.Id, request.Category, request.Text);
        }
        else if (IsForcing(product.Id))
            await PurchaseForcing(db, policy, request, product.Price, now);
        else if (product.Id is "discord" or "rename")
        {
            Require(ValidRoleName(request.Text) && request.Category.Length == 6
                && request.Category.All(Uri.IsHexDigit));
            var role = await db.Set<KabanDiscordRole>().SingleOrDefaultAsync(v => v.UserId == policy.Actor);
            Require(product.Id == "discord" ? role == null || role.Status == "rejected" : role != null && role.Status == "active",
                "cmu-kaban-already-owned");
            // The verified Discord ID comes from the existing account-link table, not from the client.
            var discord = await db.RMCLinkedAccounts.Where(l => l.PlayerId == policy.Actor).Select(l => l.DiscordId).SingleOrDefaultAsync();
            Require(discord != default, "cmu-kaban-discord-unlinked");
            if (role == null)
                db.Add(role = new KabanDiscordRole { UserId = policy.Actor });
            role.PurchaseId = purchase.Id;
            role.PreviousName = role.Name;
            role.PreviousColor = role.Color;
            role.Name = request.Text.Trim();
            role.Color = request.Category;
            role.DiscordId = discord.ToString();
            role.Status = "pending";
            role.Delivery = request.Operation;
        }
        else
            throw new KabanDenied("cmu-kaban-product-unavailable");
        db.Add(purchase);
        await ChangeBalance(db, policy.Actor, policy.Actor, -product.Price, request.Operation, now, $"purchase:{product.Id}");
    }

    public static bool ValidRoleName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < 2 or > 32 || trimmed.Any(c => !char.IsLetterOrDigit(c) && c is not ' ' and not '-' and not '_'))
            return false;
        var lower = trimmed.ToLowerInvariant();
        return !new[] { "admin", "moder", "owner", "staff", "руковод", "админ", "модер", "владел", "разраб", "developer" }
            .Any(lower.Contains);
    }

    private static async Task PrepareVoice(ServerDbContext db, Guid user, Guid purchase, string speaker, string consent)
    {
        Require(Content.Shared.Corvax.TTS.CustomTTSVoice.IsValidSpeakerName(speaker) && consent.Trim().Length >= 20);
        var voice = await db.Set<KabanVoice>().SingleOrDefaultAsync(v => v.UserId == user);
        Require(voice == null || voice.Status == "rejected", "cmu-kaban-already-owned");
        var claim = await db.Set<KabanVoiceClaim>().SingleOrDefaultAsync(c => c.Speaker == speaker);
        Require(claim == null || claim.UserId == user, "cmu-kaban-voice-exists");
        if (claim == null)
            db.Add(new KabanVoiceClaim { Speaker = speaker, UserId = user });
        if (voice == null)
            db.Add(voice = new KabanVoice { UserId = user });
        voice.PurchaseId = purchase;
        voice.Speaker = speaker;
        voice.Consent = consent.Trim();
        voice.Status = "pending";
    }

    private static async Task Arm(ServerDbContext db, KabanPolicy policy, KabanRequest request, long now)
    {
        Require(policy.Priorities && policy.Lobby, "cmu-kaban-lobby-only");
        var token = await db.Set<KabanToken>().SingleOrDefaultAsync(t => t.Id == request.Item && t.UserId == policy.Actor);
        Require(token != null && token.Status == "available" && token.Expires > now, "cmu-kaban-token-unavailable");
        Require(request.Jobs.Length >= 1 && request.Jobs.Length <= (token!.Flexible ? 3 : 1)
            && request.Jobs.Distinct().Count() == request.Jobs.Length
            && request.Jobs.All(policy.AvailableJobs.Contains));
        Require(!await db.Set<KabanToken>().AnyAsync(t => t.UserId == policy.Actor && t.Status == "reserved"), "cmu-kaban-already-armed");
        var week = now - TimeSpan.FromDays(7).Ticks;
        var uses = db.Set<KabanTokenUse>().Where(u => u.UserId == policy.Actor
            && (u.Status == "reserved" || u.At > week && u.Status == "used"));
        Require(!await uses.AnyAsync(u => u.Server == policy.Server && u.Round == policy.Round), "cmu-kaban-round-limit");
        Require(await uses.CountAsync() < 4, "cmu-kaban-week-limit");
        var command = request.Jobs.Any(policy.CommandJobs.Contains);
        if (command)
        {
            Require(await uses.CountAsync(u => u.Command) < 2, "cmu-kaban-command-limit");
            if (token.Multiplier == 5)
                Require(!await uses.AnyAsync(u => u.Command && u.Multiplier == 5), "cmu-kaban-command-five-limit");
        }
        token.Status = "reserved";
        token.Jobs = JsonSerializer.Serialize(request.Jobs);
        token.Server = policy.Server;
        token.Reservation = request.Operation;
        db.Add(new KabanTokenUse
        {
            Id = request.Operation, TokenId = token.Id, UserId = policy.Actor, Multiplier = token.Multiplier,
            Command = command, At = now, Server = policy.Server, Round = policy.Round,
        });
    }

    /// <summary>Only the owning server settles its durable reservation. Repeated settlement is harmless.</summary>
    public static async Task Settle(ServerDbContext db, string server, Guid reservation, bool used, string job, bool command, long now)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        Require(await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_lock SET revision = revision + 1 WHERE kaban_lock_id = 1") == 1,
            "cmu-kaban-storage-unavailable");
        var usage = await db.Set<KabanTokenUse>().SingleOrDefaultAsync(u => u.Id == reservation && u.Server == server);
        if (usage == null || usage.Status != "reserved")
            return;
        var token = await db.Set<KabanToken>().SingleAsync(t => t.Id == usage.TokenId);
        usage.Status = used ? "used" : "refunded";
        usage.Job = job;
        usage.Command = command;
        if (used)
            usage.At = now;
        token.Status = used ? "spent" : "available";
        token.Reservation = Guid.Empty;
        db.Add(new KabanAudit
        {
            Id = Guid.NewGuid(), Operation = reservation, Actor = Guid.Empty, Target = usage.UserId,
            Action = used ? "PriorityConsumed" : "PriorityReturned", Detail = job, At = now,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}

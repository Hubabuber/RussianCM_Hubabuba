using System;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

// CMU14: dedicated tables in the existing game database. Timestamps are UTC ticks on both providers.
public sealed class KabanWallet
{
    public Guid UserId { get; set; }
    public long Balance { get; set; }
    public long Earned { get; set; }
}

public sealed class KabanLock
{
    public int Id { get; set; }
    public long Revision { get; set; }
}

public sealed class KabanLedger
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid Actor { get; set; }
    public long Delta { get; set; }
    public long Balance { get; set; }
    public long At { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class KabanOperation
{
    public Guid Id { get; set; }
    public Guid Actor { get; set; }
    public string Fingerprint { get; set; } = "";
    public string Result { get; set; } = "";
    public long At { get; set; }
}

public sealed class KabanAward
{
    public Guid Id { get; set; }
    public Guid Recipient { get; set; }
    public Guid Initiator { get; set; }
    public int Amount { get; set; }
    public string Category { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "pending";
    public bool Exceptional { get; set; }
    public long At { get; set; }
    public long PaidAt { get; set; }
}

public sealed class KabanApproval
{
    public Guid AwardId { get; set; }
    public Guid Approver { get; set; }
    public bool Senior { get; set; }
    public long At { get; set; }
}

public sealed class KabanProduct
{
    public string Id { get; set; } = "";
    public int Price { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class KabanPurchase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ProductId { get; set; } = "";
    public int Price { get; set; }
    public long At { get; set; }
}

public sealed class KabanToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PurchaseId { get; set; }
    public int Multiplier { get; set; }
    public bool Flexible { get; set; }
    public long Expires { get; set; }
    public string Status { get; set; } = "available";
    public string Jobs { get; set; } = "[]";
    public string Server { get; set; } = "";
    public Guid Reservation { get; set; }
}

public sealed class KabanTokenUse
{
    public Guid Id { get; set; }
    public Guid TokenId { get; set; }
    public Guid UserId { get; set; }
    public string Job { get; set; } = "";
    public bool Command { get; set; }
    public int Multiplier { get; set; }
    public long At { get; set; }
    public string Status { get; set; } = "reserved";
    public string Server { get; set; } = "";
    public string Round { get; set; } = "";
}

public sealed class KabanRoundRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ProductId { get; set; } = "";
    public string Parameters { get; set; } = "";
    public int Price { get; set; }
    public long At { get; set; }
    public long CompletedAt { get; set; }
    public string Status { get; set; } = "queued";
}

public sealed class KabanVoice
{
    public Guid UserId { get; set; }
    public Guid PurchaseId { get; set; }
    public string Speaker { get; set; } = "";
    public string Consent { get; set; } = "";
    public string Status { get; set; } = "pending";
}

// A rejected/replaced personal voice must never become a public legacy voice.
public sealed class KabanVoiceClaim
{
    public string Speaker { get; set; } = "";
    public Guid UserId { get; set; }
}

public sealed class KabanDiscordRole
{
    public Guid UserId { get; set; }
    public Guid PurchaseId { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public string DiscordId { get; set; } = "";
    public string RoleId { get; set; } = "";
    public string Status { get; set; } = "pending";
    public Guid Delivery { get; set; }
    public string PreviousName { get; set; } = "";
    public string PreviousColor { get; set; } = "";
    public string Lease { get; set; } = "";
    public long LeaseUntil { get; set; }
}

public sealed class KabanAudit
{
    public Guid Id { get; set; }
    public Guid Operation { get; set; }
    public Guid Actor { get; set; }
    public Guid Target { get; set; }
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
    public long At { get; set; }
}

public static class KabanModel
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<KabanLock>().ToTable("rucm_kaban_lock").HasKey(x => x.Id);
        model.Entity<KabanLock>().HasData(new KabanLock { Id = 1 });
        model.Entity<KabanWallet>().ToTable("rucm_kaban_wallet", t =>
        {
            t.HasCheckConstraint("ck_kaban_balance", "balance >= 0");
            t.HasCheckConstraint("ck_kaban_earned", "earned >= 0");
        }).HasKey(x => x.UserId);
        model.Entity<KabanLedger>().ToTable("rucm_kaban_ledger").HasKey(x => x.Id);
        model.Entity<KabanLedger>().HasIndex(x => new { x.UserId, x.At });
        model.Entity<KabanLedger>().HasOne<KabanWallet>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanOperation>().ToTable("rucm_kaban_operation").HasKey(x => x.Id);
        model.Entity<KabanAward>().ToTable("rucm_kaban_award", t => t.HasCheckConstraint("ck_kaban_award", "amount > 0")).HasKey(x => x.Id);
        model.Entity<KabanAward>().HasIndex(x => new { x.Recipient, x.PaidAt });
        model.Entity<KabanApproval>().ToTable("rucm_kaban_approval").HasKey(x => new { x.AwardId, x.Approver });
        model.Entity<KabanApproval>().HasOne<KabanAward>().WithMany().HasForeignKey(x => x.AwardId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanProduct>().ToTable("rucm_kaban_product", t => t.HasCheckConstraint("ck_kaban_price", "price > 0")).HasKey(x => x.Id);
        model.Entity<KabanPurchase>().ToTable("rucm_kaban_purchase").HasKey(x => x.Id);
        model.Entity<KabanPurchase>().HasIndex(x => new { x.UserId, x.At });
        model.Entity<KabanPurchase>().HasOne<KabanWallet>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanPurchase>().HasOne<KabanProduct>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanToken>().ToTable("rucm_kaban_token").HasKey(x => x.Id);
        model.Entity<KabanToken>().HasIndex(x => new { x.UserId, x.Status });
        model.Entity<KabanToken>().HasOne<KabanPurchase>().WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanTokenUse>().ToTable("rucm_kaban_token_use").HasKey(x => x.Id);
        model.Entity<KabanTokenUse>().HasIndex(x => new { x.UserId, x.At });
        model.Entity<KabanTokenUse>().HasOne<KabanToken>().WithMany().HasForeignKey(x => x.TokenId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanRoundRequest>().ToTable("rucm_kaban_round_request").HasKey(x => x.Id);
        model.Entity<KabanRoundRequest>().HasIndex(x => new { x.Status, x.At });
        model.Entity<KabanVoice>().ToTable("rucm_kaban_voice").HasKey(x => x.UserId);
        model.Entity<KabanVoice>().HasIndex(x => x.Speaker).IsUnique();
        model.Entity<KabanVoiceClaim>().ToTable("rucm_kaban_voice_claim").HasKey(x => x.Speaker);
        model.Entity<KabanVoice>().HasOne<KabanPurchase>().WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanDiscordRole>().ToTable("rucm_kaban_discord_role").HasKey(x => x.UserId);
        model.Entity<KabanDiscordRole>().HasIndex(x => new { x.Status, x.LeaseUntil });
        model.Entity<KabanDiscordRole>().HasOne<KabanPurchase>().WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<KabanAudit>().ToTable("rucm_kaban_audit").HasKey(x => x.Id);
        model.Entity<KabanAudit>().HasIndex(x => new { x.Target, x.At });
        foreach (var (id, price) in new (string, int)[]
        {
            ("priority3", 60), ("priority5", 150), ("pack3", 165), ("pack5", 420),
            ("flex3", 100), ("flex5", 190), ("mode", 350), ("map", 250),
            ("govfor", 200), ("opfor", 200), ("full", 800), ("voice", 600),
            ("discord", 300), ("rename", 50),
        })
            model.Entity<KabanProduct>().HasData(new KabanProduct { Id = id, Price = price });
    }
}

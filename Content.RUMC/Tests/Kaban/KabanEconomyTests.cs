using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._RuCM.Kaban;
using Content.Server.Database;
using Content.Shared._RuCM.Kaban;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Content.IntegrationTests._RuCM.Kaban;

// CMU14: the same transaction scenarios run on both real providers, in disposable databases only.
[TestFixture(false), TestFixture(true), NonParallelizable]
public sealed partial class KabanEconomyTests(bool postgres)
{
    private string _connection = "";
    private string _database = "";
    private string _file = "";
    private readonly Guid _user = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _senior = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private KabanPolicy Policy(Guid? actor = null, bool manager = false, bool senior = false)
        => new(actor ?? _user, manager || senior, senior, true, true, true, true,
            "kaban-test", "round-one", true, ["command"], ["command", "rifleman", "medic"]);

    private ServerDbContext Open()
        => postgres
            ? new PostgresServerDbContext(new DbContextOptionsBuilder<PostgresServerDbContext>().UseNpgsql(_connection).Options)
            : new SqliteServerDbContext(new DbContextOptionsBuilder<SqliteServerDbContext>().UseSqlite(_connection).Options);

    [SetUp]
    public async Task Setup()
    {
        if (postgres)
        {
            var adminConnection = Environment.GetEnvironmentVariable("KABAN_TEST_POSTGRES");
            if (string.IsNullOrWhiteSpace(adminConnection))
                Assert.Ignore("Set KABAN_TEST_POSTGRES to a disposable PostgreSQL test cluster with CREATE DATABASE permission.");
            _database = "kaban_test_" + Guid.NewGuid().ToString("N");
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE {_database}", admin);
            await create.ExecuteNonQueryAsync();
            _connection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = _database, Pooling = false }.ConnectionString;
        }
        else
        {
            _file = Path.Combine(Path.GetTempPath(), "kaban_test_" + Guid.NewGuid().ToString("N") + ".db");
            _connection = new SqliteConnectionStringBuilder { DataSource = _file, Pooling = false, DefaultTimeout = 10 }.ConnectionString;
        }
        await using var db = Open();
        await db.Database.MigrateAsync();
        foreach (var user in new[] { _user, _manager, _senior })
            db.Player.Add(new Player
            {
                UserId = user, FirstSeenTime = Now, LastSeenTime = Now,
                LastSeenAddress = IPAddress.Loopback, LastSeenUserName = UserName(user),
            });
        db.Add(new KabanWallet { UserId = _user, Balance = 1000, Earned = 1000 });
        await db.SaveChangesAsync();
    }

    [TearDown]
    public async Task Cleanup()
    {
        if (postgres && _database.StartsWith("kaban_test_", StringComparison.Ordinal))
        {
            await using var admin = new NpgsqlConnection(Environment.GetEnvironmentVariable("KABAN_TEST_POSTGRES"));
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE {_database}", admin);
            await drop.ExecuteNonQueryAsync();
        }
        if (!postgres && _file.Length > 0)
            foreach (var path in new[] { _file, _file + "-wal", _file + "-shm" })
                File.Delete(path);
    }

    private async Task<string> Execute(KabanRequest request, KabanPolicy? policy = null, DateTime? now = null)
    {
        await using var db = Open();
        return await KabanEconomy.Execute(db, policy ?? Policy(), request, now ?? Now);
    }

    private static KabanRequest Buy(string product = "priority3", int amount = 60)
        => new() { Action = KabanAction.Purchase, Product = product, Amount = amount, Confirmed = true };

    [TestCase(false)]
    [TestCase(true)]
    public async Task ServerJsonPreservesExistingFingerprintsAndRetries(bool withAccountName)
    {
        var request = withAccountName ? Adjust(300) : Buy();
        var policy = withAccountName ? Policy(_manager, true) : Policy();
        // Freeze the persisted format from before JSON configuration moved out of the shared contract.
        var expectedJson = withAccountName ? JsonSerializer.Serialize(request) : JsonSerializer.Serialize(new
        {
            request.Operation, request.Action, request.Target, request.Item, request.Product,
            request.Text, request.Category, request.Jobs, request.Amount, request.Confirmed, request.Exceptional,
        });
        await Execute(request, policy);
        await using var db = Open();
        var operation = await db.Set<KabanOperation>().SingleAsync();
        Assert.That(operation.Fingerprint,
            Is.EqualTo(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expectedJson)))));
        Assert.That((await db.Set<KabanAudit>().SingleAsync()).Detail, Is.EqualTo(expectedJson));
        await Execute(request, policy);
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
    }

    private KabanRequest Adjust(int amount, Guid? target = null)
        => new() { Action = KabanAction.CorrectBalance, TargetName = UserName(target ?? _user), Amount = amount, Confirmed = true };

    private string UserName(Guid user)
        => user == _user ? "KabanUser" : user == _manager ? "KabanManager" : user == _senior ? "KabanSenior" : user.ToString();

    [Test]
    public async Task AccountNameIsCaseInsensitiveAndRetryDoesNotRedirectAfterRename()
    {
        var request = Adjust(300);
        request.TargetName = "  kabanUSER  ";
        await Execute(request, Policy(_manager, true));
        await using var db = Open();
        await db.Player.Where(player => player.UserId == _user).ExecuteUpdateAsync(setters =>
            setters.SetProperty(player => player.LastSeenUserName, "RenamedKabanUser"));
        await Execute(request, Policy(_manager, true));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1300));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task AmbiguousAccountNamesAndConflictingUuidCannotChangeBalances()
    {
        var forged = Adjust(300);
        forged.Target = _manager;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(forged, Policy(_manager, true)));
        await using var db = Open();
        db.Player.Add(new Player
        {
            UserId = Guid.NewGuid(), LastSeenUserName = "kabanuser", FirstSeenTime = Now,
            LastSeenTime = Now, LastSeenAddress = IPAddress.Loopback,
        });
        await db.SaveChangesAsync();
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(300), Policy(_manager, true)));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000));
    }

    [Test]
    public async Task DisabledForcingRejectsConcurrentPurchases()
    {
        await Execute(Adjust(1000, _manager), Policy(_manager, true));
        async Task<bool> Attempt(Guid actor)
        {
            try { await Execute(Buy("full", 800), Policy(actor)); return true; }
            catch (KabanDenied) { return false; }
        }
        Assert.That(await Task.WhenAll(Task.Run(() => Attempt(_user)), Task.Run(() => Attempt(_manager))), Is.All.False);
        await using var db = Open();
        Assert.That(await db.Set<KabanWallet>().Select(wallet => wallet.Balance).ToArrayAsync(), Is.All.EqualTo(1000));
        Assert.That(await db.Set<KabanPurchase>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task TwoAccountsCanReserveSameJobAndSettleIndependently()
    {
        await Execute(Adjust(1000, _manager), Policy(_manager, true));
        await Task.WhenAll(Task.Run(() => Execute(Buy())), Task.Run(() => Execute(Buy(), Policy(_manager))));
        await using var db = Open();
        var tokens = await db.Set<KabanToken>().ToArrayAsync();
        var requests = tokens.Select(token => (token.UserId, Request: new KabanRequest
            { Action = KabanAction.Arm, Item = token.Id, Jobs = ["rifleman"] })).ToArray();
        await Task.WhenAll(requests.Select(pair => Task.Run(() => Execute(pair.Request, Policy(pair.UserId)))));
        Assert.That(await db.Set<KabanToken>().CountAsync(token => token.Status == "reserved"), Is.EqualTo(2));
        await Task.WhenAll(requests.Select(pair => Task.Run(() => Settle(pair.Request.Operation, true, "rifleman"))));
        Assert.That(await db.Set<KabanToken>().CountAsync(token => token.Status == "spent"), Is.EqualTo(2));
        Assert.That(await db.Set<KabanWallet>().Select(wallet => wallet.Balance).ToArrayAsync(), Is.All.EqualTo(940));
    }

    [Test]
    public async Task SameAccountCannotReserveTwoTokensConcurrentlyOrDuplicateRoles()
    {
        await Execute(Buy("pack3", 165));
        await using var db = Open();
        var tokens = await db.Set<KabanToken>().ToArrayAsync();
        async Task<bool> Attempt(Guid token)
        {
            try { await Execute(new KabanRequest { Action = KabanAction.Arm, Item = token, Jobs = ["rifleman"] }); return true; }
            catch (KabanDenied) { return false; }
        }
        var outcomes = await Task.WhenAll(tokens.Select(token => Task.Run(() => Attempt(token.Id))));
        Assert.That(outcomes.Count(success => success), Is.EqualTo(1));
        Assert.That(await db.Set<KabanToken>().CountAsync(token => token.Status == "reserved"), Is.EqualTo(1));
        await Execute(Buy("flex3", 100));
        var flexible = await db.Set<KabanToken>().SingleAsync(token => token.Flexible);
        var duplicate = Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest
            { Action = KabanAction.Arm, Item = flexible.Id, Jobs = ["rifleman", "rifleman"] }));
        Assert.That(duplicate!.Message, Is.EqualTo("cmu-kaban-invalid"));
    }

    [Test]
    public async Task DifferentManagersCanCreditAndDeductSameAccountWithoutLostUpdates()
    {
        await Task.WhenAll(Task.Run(() => Execute(Adjust(200), Policy(_manager, true))),
            Task.Run(() => Execute(Adjust(-300), Policy(_senior, true, true))));
        await using var db = Open();
        var wallet = await db.Set<KabanWallet>().SingleAsync();
        Assert.That(wallet.Balance, Is.EqualTo(900));
        Assert.That(wallet.Earned, Is.EqualTo(1200));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task ManagerCanImmediatelyCreditAndDeductWithoutAwardLimits()
    {
        await Execute(Adjust(100000), Policy(_manager, true));
        await Execute(Adjust(-300), Policy(_manager, true));
        await using var db = Open();
        var wallet = await db.Set<KabanWallet>().SingleAsync();
        Assert.That(wallet.Balance, Is.EqualTo(100700));
        Assert.That(wallet.Earned, Is.EqualTo(101000));
        Assert.That(await db.Set<KabanAward>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanApproval>().CountAsync(), Is.Zero);
        var entries = await db.Set<KabanLedger>().ToListAsync();
        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries.All(e => e.Actor == _manager && e.UserId == _user), Is.True);
        Assert.That(await db.Set<KabanAudit>().CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task ManagerCanChangeOwnBalanceAndCreateWallet()
    {
        await Execute(Adjust(800, _manager), Policy(_manager, true));
        await Execute(Adjust(-800, _manager), Policy(_manager, true));
        await using var db = Open();
        var wallet = await db.Set<KabanWallet>().SingleAsync(w => w.UserId == _manager);
        Assert.That(wallet.Balance, Is.Zero);
        Assert.That(wallet.Earned, Is.EqualTo(800));
    }

    [TestCase(500)]
    [TestCase(-500)]
    public async Task OrdinaryPlayersCannotChangeAnyBalance(int amount)
    {
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(amount)));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(amount, _manager)));
        await using var db = Open();
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000));
        Assert.That(await db.Set<KabanOperation>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanAudit>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task InvalidAdjustmentsDoNotCreateWalletOrTransaction()
    {
        var policy = Policy(_manager, true);
        var unconfirmed = Adjust(500);
        unconfirmed.Confirmed = false;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(unconfirmed, policy));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(0), policy));
        var empty = Adjust(500);
        empty.TargetName = "";
        Assert.ThrowsAsync<KabanDenied>(() => Execute(empty, policy));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(500, Guid.NewGuid()), policy));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(int.MinValue), policy));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Adjust(-1, _manager), policy));
        await using var db = Open();
        Assert.That(await db.Set<KabanWallet>().CountAsync(), Is.EqualTo(1));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanOperation>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task ConcurrentManagerDeductionsCannotOverdraw()
    {
        async Task<bool> Attempt()
        {
            try { await Execute(Adjust(-700), Policy(_manager, true)); return true; }
            catch (KabanDenied) { return false; }
        }
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(Attempt)));
        Assert.That(results.Count(success => success), Is.EqualTo(1));
        await using var db = Open();
        var wallet = await db.Set<KabanWallet>().SingleAsync();
        Assert.That(wallet.Balance, Is.EqualTo(300));
        Assert.That(wallet.Earned, Is.EqualTo(1000));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RetriedManagerCreditIsAppliedOnceAndCannotBeRebound()
    {
        var request = Adjust(700);
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => Execute(request, Policy(_manager, true)))));
        request.Amount = 800;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(request, Policy(_manager, true)));
        request.Amount = 700;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(request, Policy(_senior, true, true)));
        await using var db = Open();
        var wallet = await db.Set<KabanWallet>().SingleAsync();
        Assert.That(wallet.Balance, Is.EqualTo(1700));
        Assert.That(wallet.Earned, Is.EqualTo(1700));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
        Assert.That(await db.Set<KabanAudit>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RetiredAwardWorkflowCannotCreateOrApproveAwards()
    {
        foreach (var action in new[] { KabanAction.Nominate, KabanAction.Approve, KabanAction.Reject })
            Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest
            {
                Action = action, Target = _user, Amount = 100, Category = "development", Text = "Legacy award request",
            }, Policy(_manager, true)));
        await using var db = Open();
        Assert.That(await db.Set<KabanAward>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
    }

    private async Task Settle(Guid id, bool used, string job, bool command = false, string server = "kaban-test")
    {
        await using var db = Open();
        await KabanEconomy.Settle(db, server, id, used, job, command, Now.Ticks);
    }

    [Test]
    public async Task CurrentModelMatchesBothProviderSnapshotsAndMigratedDatabase()
    {
        await using var db = Open();
        Assert.That(db.Database.HasPendingModelChanges(), Is.False);
        Assert.That(await db.Set<KabanProduct>().CountAsync(), Is.GreaterThan(0));
        Assert.That(await db.Set<KabanLock>().CountAsync(), Is.EqualTo(1));
        Assert.That(await db.Database.GetPendingMigrationsAsync(), Is.Empty);
    }

    [Test]
    public async Task UpgradeAndDowngradeTouchOnlyOwnedTablesAndPreserveExistingAccounts()
    {
        await using var db = Open();
        var preferences = new Preference { UserId = _user, AdminOOCColor = "#123456", SelectedCharacterSlot = 0 };
        preferences.Profiles.Add(new Profile
        {
            Preference = preferences, Slot = 0, CharacterName = "Preserved Character", FlavorText = "Preserved description",
            Sex = "Male", Gender = "Male", Species = "Human", HairName = "", HairColor = "#000000",
            FacialHairName = "", FacialHairColor = "#000000", EyeColor = "#000000", SkinColor = "#000000", ArmorPreference = "",
        });
        db.Preference.Add(preferences);
        await db.SaveChangesAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261015000000_ProfileGearFlavorText");
        Assert.That((await db.Preference.SingleAsync()).AdminOOCColor, Is.EqualTo("#123456"));
        await migrator.MigrateAsync();
        Assert.That((await db.Preference.SingleAsync()).UserId, Is.EqualTo(_user));
        Assert.That((await db.Profile.SingleAsync()).FlavorText, Is.EqualTo("Preserved description"));
        Assert.That(await db.Player.CountAsync(), Is.EqualTo(3));
        Assert.That(await db.Set<KabanWallet>().CountAsync(), Is.Zero);
        Assert.That(db.Database.HasPendingModelChanges(), Is.False);
    }

    [Test]
    public async Task PurchaseRetryIsIdempotentAndDoesNotChangeLifetimeEarned()
    {
        var purchase = Buy();
        await Execute(purchase);
        await Execute(purchase);
        await using var db = Open();
        Assert.That(await db.Set<KabanToken>().CountAsync(), Is.EqualTo(1));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
        var wallet = await db.Set<KabanWallet>().SingleAsync();
        Assert.That(wallet.Balance, Is.EqualTo(940));
        Assert.That(wallet.Earned, Is.EqualTo(1000));
    }

    [Test]
    public async Task RequestIdsCannotBeReboundToAnotherPayloadOrAccount()
    {
        var purchase = Buy();
        await Execute(purchase);
        purchase.Amount++;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(purchase));
        purchase.Amount--;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(purchase, Policy(_manager)));
    }

    [Test]
    public async Task ConcurrentPurchasesCannotOverdrawAndFailedPurchaseLeavesNoInventory()
    {
        await using (var db = Open())
            await db.Set<KabanWallet>().ExecuteUpdateAsync(s => s.SetProperty(w => w.Balance, 60));
        async Task<bool> Attempt()
        {
            try { await Execute(Buy()); return true; }
            catch (KabanDenied) { return false; }
        }
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(Attempt)));
        Assert.That(outcomes.Count(x => x), Is.EqualTo(1));
        await using var check = Open();
        Assert.That((await check.Set<KabanWallet>().SingleAsync()).Balance, Is.Zero);
        Assert.That(await check.Set<KabanPurchase>().CountAsync(), Is.EqualTo(1));
        Assert.That(await check.Set<KabanToken>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task ConcurrentRetriesCommitOnePurchase()
    {
        var purchase = Buy();
        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => Execute(purchase))));
        await using var db = Open();
        Assert.That(await db.Set<KabanPurchase>().CountAsync(), Is.EqualTo(1));
        Assert.That(await db.Set<KabanOperation>().CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UnconfirmedAndStalePricePurchasesAreRejectedWithoutDebit()
    {
        var unconfirmed = Buy();
        unconfirmed.Confirmed = false;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(unconfirmed));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Buy(amount: 1)));
        await using var db = Open();
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task DisabledModulesRejectPurchasesWithoutCharges()
    {
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Buy(), Policy() with { Shop = false }));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(Buy(), Policy() with { Priorities = false }));
        foreach (var (id, price) in new[] { ("mode", 350), ("map", 250), ("govfor", 200), ("opfor", 200), ("full", 800) })
            Assert.ThrowsAsync<KabanDenied>(() => Execute(Buy(id, price)));
        await using var db = Open();
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task TokenReservationCannotBeStolenAndCancellationReturnsIt()
    {
        await Execute(Buy());
        await using var db = Open();
        var token = await db.Set<KabanToken>().SingleAsync();
        var arm = new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["rifleman"] };
        Assert.ThrowsAsync<KabanDenied>(() => Execute(arm, Policy(_manager)));
        await Execute(arm);
        await Execute(new KabanRequest { Action = KabanAction.Disarm, Item = token.Id });
        db.ChangeTracker.Clear();
        Assert.That((await db.Set<KabanToken>().SingleAsync()).Status, Is.EqualTo("available"));
        Assert.That((await db.Set<KabanTokenUse>().SingleAsync()).Status, Is.EqualTo("refunded"));
    }

    [Test]
    public async Task ExpiredUnavailableAndMultiJobOrdinaryTokensCannotBeActivated()
    {
        await Execute(Buy());
        await using var db = Open();
        var token = await db.Set<KabanToken>().SingleAsync();
        Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["rifleman", "medic"] }));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["missing"] }));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["rifleman"] }, now: Now.AddDays(31)));
    }

    [Test]
    public async Task FlexibleTokenCanSelectThreeJobsAndSettlementIsIdempotent()
    {
        await Execute(Buy("flex3", 100));
        await using var db = Open();
        var token = await db.Set<KabanToken>().SingleAsync();
        var arm = new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["command", "rifleman", "medic"] };
        await Execute(arm);
        await Settle(arm.Operation, true, "medic");
        await Settle(arm.Operation, true, "medic");
        db.ChangeTracker.Clear();
        Assert.That((await db.Set<KabanToken>().SingleAsync()).Status, Is.EqualTo("spent"));
        Assert.That((await db.Set<KabanTokenUse>().SingleAsync()).Command, Is.False);
        Assert.That(await db.Set<KabanAudit>().CountAsync(a => a.Action == "PriorityConsumed"), Is.EqualTo(1));
    }

    [Test]
    public async Task CommandTimesFiveLimitSurvivesRoundChange()
    {
        await Execute(Buy("pack5", 420));
        await using var db = Open();
        var tokens = await db.Set<KabanToken>().OrderBy(t => t.Id).ToArrayAsync();
        var arm = new KabanRequest { Action = KabanAction.Arm, Item = tokens[0].Id, Jobs = ["command"] };
        await Execute(arm);
        await Settle(arm.Operation, true, "command", true);
        Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest { Action = KabanAction.Arm, Item = tokens[1].Id, Jobs = ["command"] }, Policy() with { Round = "round-two" }));
        await Execute(new KabanRequest { Action = KabanAction.Arm, Item = tokens[1].Id, Jobs = ["rifleman"] }, Policy() with { Round = "round-two" });
    }

    [Test]
    public async Task RollingWeekAndTwoCommandUsesAreSharedAcrossTokens()
    {
        await Execute(Buy("pack3", 165));
        await Execute(Buy("pack3", 165));
        await using var db = Open();
        var tokens = await db.Set<KabanToken>().OrderBy(t => t.Id).ToArrayAsync();
        for (var i = 0; i < 4; i++)
        {
            var job = i < 2 ? "command" : "rifleman";
            if (i == 2)
                Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest
                    { Action = KabanAction.Arm, Item = tokens[i].Id, Jobs = ["command"] }, Policy() with { Round = "command-extra" }));
            var arm = new KabanRequest { Action = KabanAction.Arm, Item = tokens[i].Id, Jobs = [job] };
            await Execute(arm, Policy() with { Round = "round-" + i });
            await Settle(arm.Operation, true, job, i < 2);
        }
        var fifth = new KabanRequest { Action = KabanAction.Arm, Item = tokens[4].Id, Jobs = ["rifleman"] };
        Assert.ThrowsAsync<KabanDenied>(() => Execute(fifth, Policy() with { Round = "fifth" }));
        await Execute(fifth, Policy() with { Round = "next-week" }, Now.AddDays(7));
    }

    [Test]
    public async Task FailureReturnsReservationWithoutSpendingAndAllowsReactivation()
    {
        await Execute(Buy());
        await using var db = Open();
        var token = await db.Set<KabanToken>().SingleAsync();
        var arm = new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["rifleman"] };
        await Execute(arm);
        await Settle(arm.Operation, true, "rifleman", server: "other-server");
        await Settle(arm.Operation, false, "rollback");
        await Execute(new KabanRequest { Action = KabanAction.Arm, Item = token.Id, Jobs = ["rifleman"] });
        Assert.That(await db.Set<KabanTokenUse>().CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task PersonalVoiceIsUniqueAndRequiresPermissionEvidence()
    {
        var buy = Buy("voice", 600);
        buy.Category = "consented_voice";
        Assert.ThrowsAsync<KabanDenied>(() => Execute(buy));
        buy.Text = "Permission document approved by the original voice owner.";
        await Execute(buy);
        await Execute(buy, Policy() with { VoiceAvailable = false });
        Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest { Action = KabanAction.ApproveVoice, Target = _user, Text = buy.Category }));
        var approve = new KabanRequest { Action = KabanAction.ApproveVoice, Target = _user, Text = buy.Category };
        await Execute(approve, Policy(_manager, true));
        await Execute(approve, Policy(_manager, true) with { VoiceVerified = false });
        var second = Buy("voice", 600);
        second.Category = "another_voice";
        second.Text = buy.Text;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(second));
    }

    [Test]
    public async Task RejectedVoiceRemainsClaimedAfterReplacementAndRefundIsIdempotent()
    {
        var first = Buy("voice", 600);
        first.Category = "first_model";
        first.Text = "Permission document approved by the original voice owner.";
        await Execute(first);
        var refund = new KabanRequest { Action = KabanAction.RejectPrivilege, Target = _user, Product = "voice" };
        await Execute(refund, Policy(_manager, true));
        await Execute(refund, Policy(_manager, true));
        var replacement = Buy("voice", 600);
        replacement.Category = "second_model";
        replacement.Text = first.Text;
        await Execute(replacement);
        await using var db = Open();
        Assert.That(await db.Set<KabanVoiceClaim>().CountAsync(), Is.EqualTo(2));
        Assert.That((await db.Set<KabanVoiceClaim>().SingleAsync(c => c.Speaker == "first_model")).UserId, Is.EqualTo(_user));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(400));
    }

    [Test]
    public async Task SpecialVoiceDecisionDoesNotCreateCurrencyOrSkipModelReview()
    {
        var request = new KabanRequest { Action = KabanAction.GrantVoice, Target = _user, Category = "special_voice",
            Text = "Senior decision and written permission from the original voice owner.", Confirmed = true };
        Assert.ThrowsAsync<KabanDenied>(() => Execute(request, Policy(_manager, true)));
        await Execute(request, Policy(_senior, true, true));
        await Execute(request, Policy(_senior, true, true) with { VoiceAvailable = false });
        await using var db = Open();
        Assert.That((await db.Set<KabanVoice>().SingleAsync()).Status, Is.EqualTo("pending"));
        Assert.That((await db.Set<KabanPurchase>().SingleAsync()).Price, Is.Zero);
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Earned, Is.EqualTo(1000));
        Assert.ThrowsAsync<KabanDenied>(() => Execute(new KabanRequest { Action = KabanAction.ApproveVoice,
            Target = _user, Text = request.Category }, Policy(_manager, true) with { VoiceVerified = false }));
    }

    [Test]
    public async Task DiscordRequiresVerifiedAccountLinkAndRejectsStaffImpersonation()
    {
        var buy = Buy("discord", 300);
        buy.Text = "Space Boars";
        buy.Category = "12ab34";
        Assert.ThrowsAsync<KabanDenied>(() => Execute(buy));
        buy.Text = "Admin team";
        Assert.ThrowsAsync<KabanDenied>(() => Execute(buy));
        await using var db = Open();
        Assert.That(await db.Set<KabanPurchase>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task JournalCannotBeEditedOrDeletedAndCorrectionsAppend()
    {
        await Execute(Buy());
        await using var db = Open();
        Assert.CatchAsync<Exception>(async () => await db.Database.ExecuteSqlRawAsync("UPDATE rucm_kaban_ledger SET delta=0"));
        Assert.CatchAsync<Exception>(async () => await db.Database.ExecuteSqlRawAsync("DELETE FROM rucm_kaban_audit"));
        await Execute(new KabanRequest { Action = KabanAction.CorrectBalance, Amount = -10,
            TargetName = UserName(_user), Text = "Correction of a verified erroneous historic reward.", Confirmed = true }, Policy(_senior, true, true));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(2));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Earned, Is.EqualTo(1000));
        if (!postgres)
            Assert.CatchAsync<Exception>(async () => await db.Database.ExecuteSqlRawAsync(
                "INSERT OR REPLACE INTO rucm_kaban_ledger SELECT * FROM rucm_kaban_ledger"));
    }
}

[TestFixture]
public sealed class KabanLotteryTests
{
    [Test]
    public void LegacyVoicesStayPublicAndPersonalVoicesRequireCurrentOwnerAndEnabledModule()
    {
        var user = Guid.NewGuid();
        var voice = new KabanVoice { UserId = user, Speaker = "personal", Status = "active" };
        Assert.That(KabanVoiceAccess.CanUse(voice, user, true, true), Is.True);
        Assert.That(KabanVoiceAccess.CanUse(voice, Guid.NewGuid(), true, true), Is.False);
        Assert.That(KabanVoiceAccess.CanUse(voice, user, false, true), Is.False);
        Assert.That(KabanVoiceAccess.CanUse(voice, user, true, false), Is.False);
        voice.Status = "rejected";
        Assert.That(KabanVoiceAccess.CanUse(voice, user, true, true), Is.False);
        Assert.That(KabanVoiceAccess.CanUse(null, user, true, true), Is.False);
        voice.Status = "legacy";
        Assert.That(KabanVoiceAccess.CanUse(voice, null, false, false), Is.True);
    }

    [Test]
    public void EveryTicketHasExactlyItsCumulativeWeightInterval()
    {
        int[] weights = [1, 3, 5, 1];
        var counts = new int[weights.Length];
        foreach (var ticket in Enumerable.Range(0, weights.Sum()))
            counts[KabanLottery.Select(weights, ticket)]++;
        Assert.That(counts, Is.EqualTo(weights));
        Assert.Throws<ArgumentOutOfRangeException>(() => KabanLottery.Select(weights, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => KabanLottery.Select(weights, weights.Sum()));
    }
}

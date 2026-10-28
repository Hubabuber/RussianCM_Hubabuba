using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._RuCM.Kaban;
using Content.Server.Database;
using Content.Shared._RuCM.Kaban;
using Microsoft.EntityFrameworkCore;

namespace Content.IntegrationTests._RuCM.Kaban;

public sealed partial class KabanEconomyTests
{
    private static readonly KabanForceOption[] ForceCatalog =
    [
        new("ModeA", "Mode A", "PlanetA", "Planet A", new() { ["GovA"] = "Gov A", ["GovB"] = "Gov B" },
            new() { ["OpA"] = "Op A", ["OpB"] = "Op B" }),
        new("ModeB", "Mode B", "PlanetB", "Planet B", new() { ["GovC"] = "Gov C" }, new() { ["OpC"] = "Op C" }),
    ];

    private KabanPolicy ForcePolicy(Guid? user = null, string server = "kaban-test")
        => Policy(user) with { Forcing = true, ForceOptions = ForceCatalog, Server = server };

    private static KabanRequest ForceBuy(string product = "mode", KabanForceChoice? choice = null)
    {
        var price = product switch { "mode" => 350, "map" => 250, "full" => 800, _ => 200 };
        choice ??= product switch
        {
            "mode" => new(Mode: "ModeA"), "map" => new(Planet: "PlanetA"),
            "govfor" => new(Govfor: "GovA"), "opfor" => new(Opfor: "OpA"),
            _ => new("ModeA", "PlanetA", "GovA", "OpA"),
        };
        return new() { Action = KabanAction.Purchase, Product = product, Text = choice.Encode(), Amount = price, Confirmed = true };
    }

    private async Task<KabanRoundRequest?> ClaimForce(string attempt = "attempt-a", string server = "kaban-test", DateTime? now = null)
    {
        await using var db = Open();
        return await KabanEconomy.ClaimForce(db, server, attempt, (now ?? Now).Ticks);
    }

    private async Task FinishForce(KabanRoundRequest request, bool used, DateTime? now = null)
    {
        var payload = KabanEconomy.ForcePayload(request)!;
        await using var db = Open();
        await KabanEconomy.FinishForce(db, payload.Server, request.Id, payload.Attempt, used, (now ?? Now).Ticks, "test");
    }

    [TestCase("mode"), TestCase("map"), TestCase("govfor"), TestCase("opfor"), TestCase("full")]
    public async Task ForcingReservesEveryProductAndReplaySurvivesDisabledModule(string product)
    {
        var buy = ForceBuy(product);
        await Execute(buy, ForcePolicy());
        await Execute(buy, Policy() with { Shop = false });
        await using var db = Open();
        var request = await db.Set<KabanRoundRequest>().SingleAsync();
        Assert.That(request.Status, Is.EqualTo("queued"));
        Assert.That(KabanEconomy.ForcePayload(request)!.Choice.Encode(), Is.EqualTo(buy.Text));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000 - buy.Amount));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Earned, Is.EqualTo(1000));
    }

    [TestCase("mode"), TestCase("map"), TestCase("govfor"), TestCase("opfor"), TestCase("full")]
    public async Task ForcingFlagIsAuthoritativeAndDoesNotCharge(string product)
    {
        Assert.ThrowsAsync<KabanDenied>(() => Execute(ForceBuy(product), ForcePolicy() with { Forcing = false }));
        await using var db = Open();
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(), Is.Zero);
    }

    [TestCase("ModeA|PlanetB|GovA|OpA"), TestCase("ModeA|PlanetA|GovC|OpA")]
    [TestCase("ModeA|PlanetA|GovA|GovA"), TestCase("ModeA|PlanetA|GovA|unknown")]
    [TestCase("../config|PlanetA|GovA|OpA"), TestCase("ModeA|PlanetA|GovA")]
    public async Task IncompatibleFullPlanNeverCreatesPartialPurchase(string parameters)
    {
        var buy = ForceBuy("full");
        buy.Text = parameters;
        Assert.ThrowsAsync<KabanDenied>(() => Execute(buy, ForcePolicy()));
        await using var db = Open();
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(), Is.Zero);
        Assert.That(await db.Set<KabanPurchase>().CountAsync(), Is.Zero);
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000));
    }

    [Test]
    public async Task SingleParameterCannotSmuggleAdditionalSelections()
    {
        Assert.ThrowsAsync<KabanDenied>(() => Execute(ForceBuy("mode", new("ModeA", "PlanetA")), ForcePolicy()));
        await using var db = Open();
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.Zero);
    }

    [Test]
    public async Task TwoPlayersPurchaseConcurrentlyAndOneFifoHeadIsClaimed()
    {
        await Execute(Adjust(1000, _manager), Policy(_manager, true));
        var first = ForceBuy();
        var second = ForceBuy("map");
        await Task.WhenAll(Execute(first, ForcePolicy(), Now), Execute(second, ForcePolicy(_manager), Now.AddSeconds(1)));
        var claims = await Task.WhenAll(ClaimForce(), ClaimForce("attempt-b"));
        Assert.That(claims.Count(q => q != null), Is.EqualTo(1));
        Assert.That(claims.Single(q => q != null)!.Id, Is.EqualTo(first.Operation));
        await using var db = Open();
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(q => q.Status == "queued"), Is.EqualTo(1));
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(q => q.Status == "applying"), Is.EqualTo(1));
        Assert.That((await db.Set<KabanWallet>().SingleAsync(w => w.UserId == _user)).Balance, Is.EqualTo(650));
        Assert.That((await db.Set<KabanWallet>().SingleAsync(w => w.UserId == _manager)).Balance, Is.EqualTo(750));
        await FinishForce(claims.Single(q => q != null)!, true);
        Assert.That(await ClaimForce(KabanEconomy.ForcePayload(claims.Single(q => q != null)!)!.Attempt), Is.Null,
            "A second claim in the same lobby cannot consume the next request.");
        var next = await ClaimForce("next-round");
        Assert.That(next!.Id, Is.EqualTo(second.Operation));
    }

    [Test]
    public async Task ConcurrentPurchasesOfOneAccountCannotReserveTwice()
    {
        async Task<bool> TryBuy()
        {
            try { await Execute(ForceBuy(), ForcePolicy()); return true; }
            catch (KabanDenied) { return false; }
        }
        var result = await Task.WhenAll(TryBuy(), TryBuy());
        Assert.That(result.Count(x => x), Is.EqualTo(1));
        await using var db = Open();
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(), Is.EqualTo(1));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(650));
    }

    [Test]
    public async Task CancellationRacingClaimNeverDuplicatesRefund()
    {
        var buy = ForceBuy();
        await Execute(buy, ForcePolicy());
        async Task Cancel()
        {
            try { await Execute(new() { Action = KabanAction.CancelRequest, Item = buy.Operation }, ForcePolicy()); }
            catch (KabanDenied) { }
        }
        var claim = ClaimForce();
        await Task.WhenAll(claim, Cancel());
        if (await claim is { } active)
        {
            await FinishForce(active, false);
            await FinishForce(active, false);
        }
        await using var db = Open();
        Assert.That((await db.Set<KabanRoundRequest>().SingleAsync()).Status, Is.EqualTo("refunded"));
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(1000));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task FailedStartRefundsExactlyOnceAndSuccessfulStartCannotBeRefunded()
    {
        await Execute(ForceBuy(), ForcePolicy());
        var failed = (await ClaimForce())!;
        await FinishForce(failed, false);
        await FinishForce(failed, false);
        await Execute(ForceBuy(), ForcePolicy());
        var success = (await ClaimForce("next-round"))!;
        await FinishForce(success, true);
        await FinishForce(success, false);
        await using var db = Open();
        Assert.That((await db.Set<KabanWallet>().SingleAsync()).Balance, Is.EqualTo(650));
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(q => q.Status == "completed"), Is.EqualTo(1));
        Assert.That(await db.Set<KabanLedger>().CountAsync(), Is.EqualTo(3));
    }

    [TestCase(6, "mode", false), TestCase(7, "mode", true)]
    [TestCase(29, "full", false), TestCase(30, "full", true)]
    public async Task ForcingCooldownCountsOnlySuccessfulCompletion(int days, string product, bool allowed)
    {
        await Execute(Adjust(1000), Policy(_manager, true));
        await Execute(ForceBuy("full"), ForcePolicy());
        await FinishForce((await ClaimForce())!, true);
        if (allowed)
            await Execute(ForceBuy(product), ForcePolicy(), Now.AddDays(days));
        else
            Assert.ThrowsAsync<KabanDenied>(() => Execute(ForceBuy(product), ForcePolicy(), Now.AddDays(days)));
        await using var db = Open();
        Assert.That(await db.Set<KabanRoundRequest>().CountAsync(q => q.Status == "queued"), Is.EqualTo(allowed ? 1 : 0));
    }

    [Test]
    public async Task RecoveryRefundsOnlyThisServersActiveClaimAndPreservesQueue()
    {
        await Execute(Adjust(1000, _manager), Policy(_manager, true));
        await Execute(ForceBuy(), ForcePolicy());
        await Execute(ForceBuy("map"), ForcePolicy(_manager, "other-server"));
        await ClaimForce();
        await using (var db = Open())
            await KabanEconomy.RecoverForce(db, "other-server", Now.Ticks);
        await using (var db = Open())
            Assert.That(await db.Set<KabanRoundRequest>().CountAsync(q => q.Status == "applying"), Is.EqualTo(1));
        await using (var db = Open())
            await KabanEconomy.RecoverForce(db, "kaban-test", Now.Ticks);
        await using (var db = Open())
            await KabanEconomy.RecoverForce(db, "kaban-test", Now.Ticks);
        await using var check = Open();
        Assert.That((await check.Set<KabanWallet>().SingleAsync(w => w.UserId == _user)).Balance, Is.EqualTo(1000));
        Assert.That(await check.Set<KabanRoundRequest>().CountAsync(q => q.Status == "queued"), Is.EqualTo(1));
    }
}

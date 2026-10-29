using System;
using System.IO;
using System.Linq;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Eui;
using NetSerializer;

namespace Content.IntegrationTests._RuCM.Kaban;

/// <summary>Exercise the serializer used by MsgEuiState, including nested account and product data.</summary>
[TestFixture]
public sealed class KabanSerializationTests
{
    private static T Roundtrip<T>(T value)
    {
        var sender = new Serializer(new[] { typeof(KabanState), typeof(KabanRequest), typeof(KabanOpenEvent) });
        var receiver = new Serializer(sender.GetTypeMap());
        using var stream = new MemoryStream();
        sender.Serialize(stream, value);
        stream.Position = 0;
        var decoded = receiver.Deserialize(stream);
        Assert.That(stream.Position, Is.EqualTo(stream.Length), "The receiver must consume the complete packet.");
        return (T) decoded;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StateRoundtripsEveryNestedRecordAndOptionalJobList(bool withJobs)
    {
        var viewer = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var view = new KabanState
        {
            Viewer = viewer, Response = operation, Balance = 3000000000L, Earned = 4000000000L,
            Manager = true, Senior = true, PrioritiesEnabled = true, Lobby = true, Result = "cmu-kaban-ok",
            Products = [new("priority3", 60, true, ""), new("full", 800, false, "cmu-kaban-force-blocked")],
            Items = [new(operation, "token", "", DateTime.UtcNow.Ticks, "reserved", 3, true,
                withJobs ? ["role-a", "role-b"] : null, "detail", 3, "TestContributor")],
            History = [new(operation, DateTime.UtcNow.Ticks, "manager-adjustment:Тест", -100)],
            Awards = [new(operation, viewer, viewer, 100, "testing", "historical", "paid", [viewer], DateTime.UtcNow.Ticks)],
            Audit = [new(operation, DateTime.UtcNow.Ticks, "audit", 0)],
            Jobs = new() { ["role-a"] = "Медбрат · Колония", ["role-b"] = "Медбрат · GOVFOR" },
            Accounts = new() { [viewer] = "TestContributor" },
            Wallets = [new(viewer, 3000000000L, 4000000000L)],
            ForcingEnabled = true,
            ForceOptions = [new("mode", "Режим", "planet", "Планета", new() { ["gov"] = "GOVFOR" }, new() { ["op"] = "OPFOR" })],
        };
        // This is the polymorphic base type read by MsgEuiState.
        var decoded = (KabanState) Roundtrip<EuiStateBase>(view);
        Assert.That(decoded.Viewer, Is.EqualTo(viewer));
        Assert.That(decoded.Response, Is.EqualTo(operation));
        Assert.That(decoded.Balance, Is.EqualTo(view.Balance));
        Assert.That(decoded.Earned, Is.EqualTo(view.Earned));
        Assert.That(decoded.Manager && decoded.Senior && decoded.PrioritiesEnabled && decoded.Lobby, Is.True);
        Assert.That(decoded.Products, Is.EqualTo(view.Products));
        Assert.That(decoded.Items.Single().Jobs, Is.EqualTo(view.Items.Single().Jobs));
        Assert.That(decoded.Items.Single().Detail, Is.EqualTo("detail"));
        Assert.That(decoded.Items.Single().Position, Is.EqualTo(3));
        Assert.That(decoded.Items.Single().OwnerName, Is.EqualTo("TestContributor"));
        Assert.That(decoded.History, Is.EqualTo(view.History));
        Assert.That(decoded.Awards.Single().Approvals, Is.EqualTo(new[] { viewer }));
        Assert.That(decoded.Audit, Is.EqualTo(view.Audit));
        Assert.That(decoded.Jobs, Is.EquivalentTo(view.Jobs));
        Assert.That(decoded.Accounts, Is.EquivalentTo(view.Accounts));
        Assert.That(decoded.Wallets, Is.EqualTo(view.Wallets));
        Assert.That(decoded.ForcingEnabled, Is.True);
        Assert.That(decoded.ForceOptions.Single().ModeName, Is.EqualTo("Режим"));
        Assert.That(decoded.ForceOptions.Single().Govfor, Is.EquivalentTo(view.ForceOptions.Single().Govfor));
        Assert.That(decoded.ForceOptions.Single().Opfor, Is.EquivalentTo(view.ForceOptions.Single().Opfor));
    }

    [TestCase(null)]
    [TestCase("TestContributor")]
    public void RequestRoundtripsNullableAccountNameAndOperation(string? name)
    {
        var request = new KabanRequest
        {
            Action = KabanAction.CorrectBalance, TargetName = name, Amount = -500, Confirmed = true,
            Item = Guid.NewGuid(), Text = "Тест", Category = "category", Product = "product", Jobs = ["role-a", "role-b"],
        };
        var decoded = (KabanRequest) Roundtrip<EuiMessageBase>(request);
        Assert.That(decoded.Operation, Is.EqualTo(request.Operation));
        Assert.That(decoded.Action, Is.EqualTo(request.Action));
        Assert.That(decoded.TargetName, Is.EqualTo(name));
        Assert.That(decoded.Amount, Is.EqualTo(-500));
        Assert.That(decoded.Confirmed, Is.True);
        Assert.That(decoded.Item, Is.EqualTo(request.Item));
        Assert.That(decoded.Text, Is.EqualTo(request.Text));
        Assert.That(decoded.Category, Is.EqualTo(request.Category));
        Assert.That(decoded.Product, Is.EqualTo(request.Product));
        Assert.That(decoded.Jobs, Is.EqualTo(request.Jobs));
    }
}

using Content.IntegrationTests.Pair;
using Content.Server._CMU14.Acquaintance;
using Content.Server.Mind;
using Content.Shared._CMU14.Acquaintance;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.NPC.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests._CMU14.Acquaintance;

[TestFixture]
public sealed class AcquaintanceTest
{
    [Test]
    public async Task ConfirmationEventStoresIntroducedName()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
            DummyTicker = false,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var acquaintance = entMan.System<AcquaintanceSystem>();
        var minds = entMan.System<MindSystem>();

        await server.WaitAssertion(() =>
        {
            var speaker = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var listener = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);

            minds.TransferTo(minds.CreateMind(null, "Speaker"), speaker);
            minds.TransferTo(minds.CreateMind(null, "Listener"), listener);

            var claimedName = "Test Name";
            var voiceName = entMan.GetComponent<MetaDataComponent>(speaker).EntityName;

            entMan.EventBus.RaiseLocalEvent(
                listener,
                new RememberIntroductionConfirmEvent(
                    entMan.GetNetEntity(speaker),
                    claimedName,
                    voiceName,
                    true));

            Assert.That(acquaintance.GetPerceivedFaceName(listener, speaker), Is.EqualTo(claimedName));
            Assert.That(acquaintance.GetPerceivedVoiceName(listener, speaker, voiceName), Is.EqualTo(claimedName));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IntroductionSeparatelyTeachesFaceAndVoice()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
            DummyTicker = false,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var acquaintance = entMan.System<AcquaintanceSystem>();
        var minds = entMan.System<MindSystem>();

        await server.WaitAssertion(() =>
        {
            var speaker = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var listener = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);

            minds.TransferTo(minds.CreateMind(null, "Speaker"), speaker);
            minds.TransferTo(minds.CreateMind(null, "Listener"), listener);

            var claimedName = entMan.GetComponent<MetaDataComponent>(speaker).EntityName;
            var unknownFace = acquaintance.GetPerceivedFaceName(listener, speaker);
            var unknownVoice = acquaintance.GetPerceivedVoiceName(listener, speaker, entMan.GetComponent<MetaDataComponent>(speaker).EntityName);

            Assert.That(unknownFace, Is.Not.EqualTo(claimedName));
            Assert.That(unknownVoice, Is.Not.EqualTo(claimedName));
            var coloredName = acquaintance.GetColoredChatName(speaker, unknownFace);
            Assert.That(coloredName, Does.StartWith("[color=#"));
            Assert.That(coloredName, Does.Contain(unknownFace));
            Assert.That(coloredName, Does.EndWith("[/color]"));
            Assert.That(
                acquaintance.GetColoredChatName(speaker, unknownFace),
                Is.EqualTo(coloredName));

            acquaintance.Introduce(speaker, listener);

            Assert.That(acquaintance.GetPerceivedFaceName(listener, speaker), Is.EqualTo(claimedName));
            Assert.That(
                acquaintance.GetPerceivedVoiceName(listener, speaker, entMan.GetComponent<MetaDataComponent>(speaker).EntityName),
                Is.EqualTo(claimedName));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CoveredFaceAndChangedVoiceAreNotRecognized()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
            DummyTicker = false,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var acquaintance = entMan.System<AcquaintanceSystem>();
        var minds = entMan.System<MindSystem>();

        await server.WaitAssertion(() =>
        {
            var speaker = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var listener = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);

            minds.TransferTo(minds.CreateMind(null, "Speaker"), speaker);
            minds.TransferTo(minds.CreateMind(null, "Listener"), listener);

            var claimedName = entMan.GetComponent<MetaDataComponent>(speaker).EntityName;
            var normalVoice = entMan.GetComponent<MetaDataComponent>(speaker).EntityName;
            acquaintance.Introduce(speaker, listener);

            var blocker = entMan.EnsureComponent<IdentityBlockerComponent>(speaker);
            blocker.Enabled = true;
            blocker.Coverage = IdentityBlockerCoverage.FULL;

            Assert.That(acquaintance.GetPerceivedFaceName(listener, speaker), Is.Not.EqualTo(claimedName));
            Assert.That(acquaintance.GetPerceivedVoiceName(listener, speaker, normalVoice), Is.EqualTo(claimedName));
            Assert.That(acquaintance.GetPerceivedVoiceName(listener, speaker, "Changed Voice"), Is.Not.EqualTo(claimedName));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ServiceFactionsKnowEachOtherButColonistsDoNot()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = false,
            Dirty = true,
            DummyTicker = false,
        });

        var server = pair.Server;
        var entMan = server.EntMan;
        var acquaintance = entMan.System<AcquaintanceSystem>();

        await server.WaitAssertion(() =>
        {
            var govfor = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var opfor = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var weyu = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var colonist = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);
            var otherColonist = entMan.SpawnEntity("CMMobHuman", MapCoordinates.Nullspace);

            entMan.EnsureComponent<NpcFactionMemberComponent>(govfor).Factions.Add("GOVFOR");
            entMan.EnsureComponent<NpcFactionMemberComponent>(opfor).Factions.Add("OPFOR");
            entMan.EnsureComponent<NpcFactionMemberComponent>(weyu).Factions.Add("AUWeYu");
            entMan.EnsureComponent<NpcFactionMemberComponent>(colonist).Factions.Add("AUColonist");
            entMan.EnsureComponent<NpcFactionMemberComponent>(otherColonist).Factions.Add("AUColonist");

            var govforName = entMan.GetComponent<MetaDataComponent>(govfor).EntityName;
            var opforName = entMan.GetComponent<MetaDataComponent>(opfor).EntityName;
            var weyuName = entMan.GetComponent<MetaDataComponent>(weyu).EntityName;
            var otherColonistName = entMan.GetComponent<MetaDataComponent>(otherColonist).EntityName;

            Assert.That(acquaintance.GetPerceivedFaceName(govfor, opfor), Is.EqualTo(opforName));
            Assert.That(acquaintance.GetPerceivedFaceName(opfor, weyu), Is.EqualTo(weyuName));
            Assert.That(acquaintance.GetPerceivedVoiceName(weyu, govfor, govforName), Is.EqualTo(govforName));

            Assert.That(acquaintance.GetPerceivedFaceName(colonist, otherColonist), Is.Not.EqualTo(otherColonistName));
            Assert.That(acquaintance.GetPerceivedFaceName(govfor, colonist), Is.Not.EqualTo(entMan.GetComponent<MetaDataComponent>(colonist).EntityName));
            Assert.That(acquaintance.GetPerceivedVoiceName(govfor, opfor, "Changed Voice"), Is.Not.EqualTo(opforName));
        });

        await pair.CleanReturnAsync();
    }
}

#pragma warning disable RA0002 // Regression coverage inspects vocal and audio state.

using Content.IntegrationTests.Fixtures;
using Content.Server.Chat.Systems;
using Content.Server.CMU14.Emotes;
using Content.Shared._RMC14.CCVar;
using Content.Shared.CMU14.Origin;
using Content.Shared.CMU14.Traits.Quirks;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Emotes;

[TestFixture]
public sealed class CMUUPPRareVoicesTest : GameTest
{
    [Test]
    public async Task AllUppOriginsGrantRareVoicesIndependentlyOfAllegiance()
    {
        var map = await Pair.CreateTestMap();
        var entities = new List<EntityUid>();
        try
        {
            await Server.WaitAssertion(() =>
            {
                foreach (var origin in SProtoMan.EnumeratePrototypes<OriginPrototype>())
                {
                    var source = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
                    entities.Add(source);
                    var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
                        .WithOrigin(origin.ID)
                        .WithAllegiance("UA");
                    RaiseSpawn(source, profile);
                    Assert.That(SEntMan.HasComponent<CMUUPPRareVoicesComponent>(source),
                        Is.EqualTo(origin.ID.StartsWith("UPP", StringComparison.Ordinal)), origin.ID);
                }

                var other = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
                entities.Add(other);
                RaiseSpawn(other, HumanoidCharacterProfile.DefaultWithSpecies("Human")
                    .WithOrigin("UAColonies").WithAllegiance("UPP"));
                Assert.That(SEntMan.HasComponent<CMUUPPRareVoicesComponent>(other), Is.False,
                    "UPP allegiance alone must not grant origin voices");
                RaiseSpawn(other, HumanoidCharacterProfile.DefaultWithSpecies("Human").WithOrigin(null));
                Assert.That(SEntMan.HasComponent<CMUUPPRareVoicesComponent>(other), Is.False);
            });
        }
        finally
        {
            foreach (var entity in entities)
                await Pair.DeleteEntityTreeLeafFirst(entity);
        }
    }

    [TestCase("Warcry", true, false, false)]
    [TestCase("Corpsman", true, false, false)]
    [TestCase("Warcry", false, false, false)]
    [TestCase("Corpsman", false, false, false)]
    [TestCase("Cough", true, false, false)]
    [TestCase("Warcry", true, true, false)]
    [TestCase("Warcry", true, false, true)]
    public async Task RareVoicesReplaceOnlyEligibleEmotesAndRespectMute(
        string emote, bool rare, bool mute, bool filtered)
    {
        var map = await Pair.CreateTestMap();
        var originalAttached = ServerSession!.AttachedEntity;
        var originalSelf = Client.CfgMan.GetCVar(RMCCVars.RMCPlayEmotesYourself);
        var originalHuman = Client.CfgMan.GetCVar(RMCCVars.RMCPlayEmotesHuman);
        EntityUid source = default;
        try
        {
            await Client.WaitPost(() =>
            {
                Client.CfgMan.SetCVar(RMCCVars.RMCPlayEmotesYourself, !filtered);
                Client.CfgMan.SetCVar(RMCCVars.RMCPlayEmotesHuman, true);
            });
            await Server.WaitPost(() =>
            {
                source = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
                RaiseSpawn(source, HumanoidCharacterProfile.DefaultWithSpecies("Human").WithOrigin("UPPRussia"));
                SEntMan.GetComponent<CMUUPPRareVoicesComponent>(source).Chance = rare ? 1 : 0;
                Server.System<HumanoidProfileSystem>().SetVoice(source, "RMCMaleHuman");
                if (mute)
                    SEntMan.AddComponent<CMUMuteComponent>(source);
                Server.PlayerMan.SetAttachedEntity(ServerSession!, source);
            });
            await Pair.RunUntilSynced();
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.GetComponent<VocalComponent>(source).EmoteSounds?.Id,
                    Is.EqualTo("RMCMaleHuman"));
                var before = AttachedAudio(source);
                Assert.That(Server.System<ChatSystem>().TryEmoteWithChat(source, emote), Is.True);
                var created = AttachedAudio(source).Except(before).ToArray();
                Assert.That(created, Has.Length.EqualTo(mute || filtered ? 0 : 1),
                    "one emote must never play both a rare voice and the ordinary voice");
                if (mute || filtered)
                    return;

                var file = SEntMan.GetComponent<AudioComponent>(created.Single()).FileName;
                var expectedRare = rare && (emote is "Warcry" or "Corpsman");
                Assert.That(file.StartsWith("/Audio/CMU14/Voice/UPP/", StringComparison.Ordinal),
                    Is.EqualTo(expectedRare), file);
                if (expectedRare)
                    Assert.That(file, Does.Contain(emote == "Warcry" ? "upp_warcry_" : "upp_medic_"));
            });
        }
        finally
        {
            await Client.WaitPost(() =>
            {
                Client.CfgMan.SetCVar(RMCCVars.RMCPlayEmotesYourself, originalSelf);
                Client.CfgMan.SetCVar(RMCCVars.RMCPlayEmotesHuman, originalHuman);
            });
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, originalAttached));
            if (source.IsValid())
                await Pair.DeleteEntityTreeLeafFirst(source);
        }
    }

    private void RaiseSpawn(EntityUid source, HumanoidCharacterProfile profile)
    {
        SEntMan.EventBus.RaiseEvent(EventSource.Local, new PlayerSpawnCompleteEvent(
            source, ServerSession!, null, false, true, 1, EntityUid.Invalid, profile));
    }

    private EntityUid[] AttachedAudio(EntityUid source)
    {
        return SEntMan.QueryEntities<AudioComponent>()
            .Where(audio => SEntMan.GetComponent<TransformComponent>(audio.Owner).ParentUid == source)
            .Select(audio => audio.Owner).ToArray();
    }
}

#pragma warning restore RA0002

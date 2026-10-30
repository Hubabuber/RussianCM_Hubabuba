using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Round;
using Content.Server.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._CMU14.Round;

[TestFixture]
public sealed class DefaultRoundSetupTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        InLobby = true,
        Fresh = true,
    };

    [Test]
    public async Task DefaultSetupIsInsurgencyStableGarrisonUpp()
    {
        await Server.WaitAssertion(() =>
        {
            var ticker = Server.System<GameTicker>();
            var auRound = Server.System<AuRoundSystem>();
            var platoonSpawn = Server.System<PlatoonSpawnRuleSystem>();

            Assert.That(ticker.Preset?.ID, Is.EqualTo("Insurgency"));
            Assert.That(auRound.GetSelectedPlanetId(), Is.EqualTo("CMUPlanetStableGarrisonRedux"));
            Assert.That(platoonSpawn.SelectedGovforPlatoon?.ID, Is.EqualTo("UPP"));
        });
    }
}

using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.Administration.Managers;
using Content.Shared._RuCM.Qualifications;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture]
public sealed class CMUQualificationReadTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task RepeatedRecordAccessChecksDoNotCloneUnrelatedPlayerHistory()
    {
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            if (admins.GetAdminData(ServerSession) != null)
                admins.DeAdmin(ServerSession);

            var seed = new QualificationStore();
            for (var i = 0; i < 5000; i++)
            {
                var id = Guid.NewGuid();
                seed.Players[id] = new PlayerTrainingState { Player = id };
            }

            Server.ResolveDependency<IEntityManager>().System<QualificationSystem>()
                .ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            var system = Server.ResolveDependency<IEntityManager>().System<QualificationSystem>();
            for (var i = 0; i < 5; i++)
                system.CanBrowseRecords(ServerSession);

            var allowed = false;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
                allowed |= system.CanBrowseRecords(ServerSession);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            TestContext.Progress.WriteLine($"100 record access checks allocated {allocated:N0} bytes.");

            Assert.That(allowed, Is.False, "Unprivileged players must not gain access to other records.");
            Assert.That(allocated, Is.LessThan(256 * 1024),
                "Checking 100 players must not copy the qualification store for each permission lookup.");
        });
    }
}

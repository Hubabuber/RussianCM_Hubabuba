using System;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;

namespace Content.Tests.CMU14.Qualifications;

[TestFixture]
public sealed class CMUQualificationPermissionTest
{
    [Test, Combinatorial]
    public async Task AuthorityPreservesPermissionAndParticipationRules(
        [Values] bool administrator,
        [Values] bool management,
        [Values] bool participant,
        [Values("crew", "officer", "co", "both")] string job)
    {
        var player = Guid.NewGuid();
        var verified = Guid.NewGuid();
        var context = new TrainingContext(player, "character", job, 42, "test", DateTimeOffset.UtcNow);
        var seed = new QualificationStore
        {
            OfficerJobs = new() { "officer", "both" },
            CommandingOfficerJobs = new() { "co", "both" },
        };
        if (management)
            seed.Management.Add(player);

        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(seed);
        var authority = service.GetAuthority(context, administrator, participant, verified);

        Assert.Multiple(() =>
        {
            Assert.That(authority.Context, Is.EqualTo(context));
            Assert.That(authority.Administrator, Is.EqualTo(administrator));
            Assert.That(authority.Management, Is.EqualTo(management));
            Assert.That(authority.CurrentParticipant, Is.EqualTo(participant));
            Assert.That(authority.CurrentOfficer, Is.EqualTo(participant && job is "officer" or "both"));
            Assert.That(authority.CurrentCo, Is.EqualTo(participant && job is "co" or "both"));
            Assert.That(authority.VerifiedInitiator, Is.EqualTo(verified));
        });
    }

    [Test]
    public async Task RefreshedPermissionsImmediatelyReplacePreviousAuthority()
    {
        var player = Guid.NewGuid();
        var context = new TrainingContext(player, "character", "command", 42, "test", DateTimeOffset.UtcNow);
        var seed = new QualificationStore
        {
            Management = new() { player },
            OfficerJobs = new() { "command" },
            CommandingOfficerJobs = new() { "command" },
            Instructors = new() { [player] = new(true, true, true, new(), player, context.At, player, context.At) },
        };
        var repository = new MemoryQualificationRepository();
        var service = new QualificationService(repository);
        await service.Initialize(seed);

        Assert.That(service.GetAuthority(context, false, true),
            Is.EqualTo(new QualificationAuthority(context, false, true, true, true, true)));
        Assert.That(service.IsActiveInstructor(player), Is.True);

        var next = service.Snapshot();
        next.Management.Clear();
        next.OfficerJobs.Clear();
        next.CommandingOfficerJobs.Clear();
        next.Instructors.Clear();
        next.Revision++;
        await repository.Save(next, service.Revision);
        await service.Refresh();

        Assert.That(service.GetAuthority(context, false, true),
            Is.EqualTo(new QualificationAuthority(context, false, false, false, false, true)));
        Assert.That(service.IsActiveInstructor(player), Is.False);
    }

    [Test]
    public async Task InstructorMutationImmediatelyUpdatesReadAccess()
    {
        var player = Guid.NewGuid();
        var context = new TrainingContext(Guid.NewGuid(), "admin", "", 42, "test", DateTimeOffset.UtcNow);
        var administrator = new QualificationAuthority(context, true, false, false, false, false);
        var service = new QualificationService(new MemoryQualificationRepository());
        await service.Initialize(new QualificationStore());
        Assert.That(service.IsActiveInstructor(player), Is.False);

        foreach (var active in new[] { true, false })
        {
            var accreditation = new InstructorAccreditation(active, true, true, new(),
                context.Actor, context.At, context.Actor, context.At);
            await service.Apply(administrator, QualificationAction.SaveInstructor, new()
            {
                Target = player,
                Revision = service.Revision,
                Reason = "Update accreditation",
                Payload = JsonSerializer.Serialize(accreditation),
            });
            Assert.That(service.IsActiveInstructor(player), Is.EqualTo(active));
        }
    }
}

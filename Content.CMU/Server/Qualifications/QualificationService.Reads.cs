using System;
using System.Threading;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed partial class QualificationService
{
    public QualificationAuthority GetAuthority(TrainingContext context, bool administrator, bool currentParticipant,
        Guid? verifiedInitiator = null)
    {
        // The published store is copy-on-write. Read one version without copying unrelated player history.
        var cache = Volatile.Read(ref _cache);
        return new(context, administrator, cache.Management.Contains(context.Actor),
            currentParticipant && cache.OfficerJobs.Contains(context.Job),
            currentParticipant && cache.CommandingOfficerJobs.Contains(context.Job),
            currentParticipant, verifiedInitiator);
    }

    public bool IsActiveInstructor(Guid player) =>
        Volatile.Read(ref _cache).Instructors.TryGetValue(player, out var instructor) && instructor.Active;
}

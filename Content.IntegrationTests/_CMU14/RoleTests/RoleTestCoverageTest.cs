using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RuMC14.RoleTests;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.IntegrationTests._CMU14.RoleTests;

public sealed class RoleTestCoverageTest
{
    [Test]
    public async Task PersonalizationJobsHaveQuestionPools()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var prototypes = server.ProtoMan;

        await server.WaitAssertion(() =>
        {
            var missing = new SortedSet<string>();
            var insufficient = new SortedSet<string>();
            var restrictedRoleKnowledge = new SortedSet<string>();
            var whitelisted = new SortedSet<string>();
            var questions = prototypes.EnumeratePrototypes<RoleTestQuestionPrototype>().ToList();
            var testPools = prototypes.EnumeratePrototypes<RoleTestQuestionPoolPrototype>().ToList();
            var testedJobIds = testPools
                .Select(pool => pool.Job.Id)
                .ToHashSet();

            foreach (var department in prototypes.EnumeratePrototypes<DepartmentPrototype>())
            {
                if (department.EditorHidden)
                    continue;

                foreach (var jobId in department.Roles)
                {
                    if (!prototypes.TryIndex<JobPrototype>(jobId, out var job) ||
                        !job.ID.StartsWith("AU14Job") ||
                        !job.SetPreference ||
                        job.Hidden ||
                        RoleTestShared.IsRoleTestExempt(job))
                    {
                        continue;
                    }

                    if (!prototypes.TryIndex<RoleTestQuestionPoolPrototype>(job.ID, out var pool))
                    {
                        missing.Add(job.ID);
                        continue;
                    }

                    if (job.Whitelisted)
                        whitelisted.Add(job.ID);
                }
            }

            foreach (var pool in testPools)
            {
                var job = prototypes.Index(pool.Job);
                var configuredPools = pool.GetPools();
                configuredPools.Remove(RoleTestShared.CommonPool);
                configuredPools.Remove(RoleTestShared.LawPool);
                if (configuredPools.Count == 0)
                {
                    insufficient.Add($"{job.ID}: has no configured question pools");
                    continue;
                }

                var jobSpecific = questions
                    .Where(question => RoleTestShared.IsJobSpecificQuestion(question.ID, job.ID))
                    .DistinctBy(question => NormalizeQuestionText(question.Text), StringComparer.OrdinalIgnoreCase)
                    .Count();
                if (jobSpecific is > 0 and < RoleTestShared.RequiredJobSpecificQuestionCount)
                {
                    insufficient.Add(
                        $"{job.ID}: has {jobSpecific} job-specific questions, " +
                        $"requires {RoleTestShared.RequiredJobSpecificQuestionCount}");
                }

                foreach (var configuredPool in configuredPools)
                {
                    var available = questions
                        .Where(question => question.Pools.Contains(configuredPool))
                        .DistinctBy(question => NormalizeQuestionText(question.Text), StringComparer.OrdinalIgnoreCase)
                        .Count();
                    var requiredConfigured = RoleTestShared.GetRequiredConfiguredPoolQuestionCount(pool.Responsibility);

                    if (available < requiredConfigured)
                        insufficient.Add($"{job.ID}: pool {configuredPool} has {available}, requires {requiredConfigured}");
                }

                var testPoolsForJob = new HashSet<string>(configuredPools)
                {
                    RoleTestShared.CommonPool,
                };
                if (RoleTestShared.RequiresLaw(job))
                    testPoolsForJob.Add(RoleTestShared.LawPool);

                var eligibleQuestions = questions
                    .Where(question => question.Pools.Overlaps(testPoolsForJob))
                    .Where(question => IsQuestionEligibleForJob(question, job, configuredPools))
                    .Where(question => !IsQuestionSpecificToAnotherJob(question.ID, job.ID, testedJobIds))
                    .Where(question =>
                        !question.Pools.Contains(RoleTestShared.CommonPool) ||
                        RoleTestShared.IsGeneralCommonQuestion(question.ID))
                    .ToList();
                var totalUnique = eligibleQuestions
                    .DistinctBy(question => NormalizeQuestionText(question.Text), StringComparer.OrdinalIgnoreCase)
                    .Count();
                var questionCount = RoleTestShared.GetQuestionCount(pool.Responsibility);
                if (totalUnique < questionCount)
                    insufficient.Add($"{job.ID}: has {totalUnique} unique questions, requires {questionCount}");

                if (RoleTestShared.IsCivilianJob(job))
                {
                    foreach (var question in eligibleQuestions.Where(ContainsRestrictedRoleKnowledge))
                    {
                        restrictedRoleKnowledge.Add($"{job.ID}: {question.ID}");
                    }
                }
            }

            Assert.That(missing, Is.Empty,
                $"CMU personalization jobs without role test pools:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
            Assert.That(whitelisted, Is.Empty,
                $"CMU personalization jobs must use role tests instead of whitelists:{Environment.NewLine}{string.Join(Environment.NewLine, whitelisted)}");
            Assert.That(insufficient, Is.Empty,
                $"CMU personalization jobs without enough role questions:{Environment.NewLine}{string.Join(Environment.NewLine, insufficient)}");
            Assert.That(restrictedRoleKnowledge, Is.Empty,
                $"Non-combat role-specific questions must not require ROE/SOP knowledge:{Environment.NewLine}{string.Join(Environment.NewLine, restrictedRoleKnowledge)}");
            Assert.That(testPools.Select(pool => pool.Responsibility).Distinct(), Is.EquivalentTo(
                new[]
                {
                    RoleTestResponsibility.Low,
                    RoleTestResponsibility.Medium,
                    RoleTestResponsibility.High,
                }),
                "Role test pools must include low, medium, and high responsibility roles.");

            var scientistCommonQuestions = questions
                .Where(question => question.Pools.Contains(RoleTestShared.CommonPool))
                .Where(question => RoleTestShared.IsGeneralCommonQuestion(question.ID))
                .ToList();
            Assert.That(scientistCommonQuestions, Has.None.Matches<RoleTestQuestionPrototype>(question =>
                question.Text.Contains("SOP", StringComparison.OrdinalIgnoreCase)));

            Assert.That(GetConfiguredPools(testPools, "AU14JobCivilianScientist"), Is.EquivalentTo(new[]
            {
                "group:cmu-technical",
            }));
            Assert.That(GetConfiguredPools(testPools, "AU14JobCivilianPhysician"), Is.EquivalentTo(new[]
            {
                "group:cmu-medical",
            }));
            Assert.That(GetConfiguredPools(testPools, "AU14JobCivilianHeadPhysician"), Is.EquivalentTo(new[]
            {
                "group:cmu-colony-command",
                "group:cmu-medical",
            }));
            Assert.That(GetConfiguredPools(testPools, "AU14JobGOVFORPlatoonCorpsman"), Is.EquivalentTo(new[]
            {
                "group:cmu-medical",
                "group:cmu-military-command",
            }));
        });

        await pair.CleanReturnAsync();
    }

    private static string NormalizeQuestionText(string text)
    {
        return string.Join(' ', text.Split((char[]) null!, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsQuestionEligibleForJob(
        RoleTestQuestionPrototype question,
        JobPrototype job,
        HashSet<string> configuredPools)
    {
        if (question.Pools.Contains(RoleTestShared.CommonPool))
            return true;

        if (RoleTestShared.IsJobSpecificQuestion(question.ID, job.ID))
            return true;

        if (RoleTestShared.RequiresLaw(job) && question.Pools.Contains(RoleTestShared.LawPool))
            return true;

        return question.Pools.Overlaps(configuredPools);
    }

    private static bool IsQuestionSpecificToAnotherJob(
        string questionId,
        string jobId,
        HashSet<string> testedJobIds)
    {
        foreach (var otherJobId in testedJobIds)
        {
            if (otherJobId == jobId)
                continue;

            if (RoleTestShared.IsJobSpecificQuestion(questionId, otherJobId))
                return true;
        }

        return false;
    }

    private static HashSet<string> GetConfiguredPools(
        IEnumerable<RoleTestQuestionPoolPrototype> pools,
        string jobId)
    {
        var configuredPools = pools
            .Single(pool => pool.Job.Id == jobId)
            .GetPools();
        configuredPools.Remove(RoleTestShared.CommonPool);
        configuredPools.Remove(RoleTestShared.LawPool);

        return configuredPools;
    }

    private static bool ContainsRestrictedRoleKnowledge(RoleTestQuestionPrototype question)
    {
        var caseInsensitiveTerms = new[]
        {
            "ROE",
            "SOP",
            "GOVFOR",
            "OPFOR",
            "AO",
            "\u0441\u043f\u0430\u0441",
            "\u043d\u0435\u043a\u043e\u043c\u0431\u0430\u0442",
            "\u043a\u043e\u043c\u0431\u0430\u0442",
            "\u0437\u0430\u0449\u0438\u0449\u0451\u043d\u043d",
            "\u0437\u0430\u0449\u0438\u0449\u0435\u043d\u043d",
            "\u043f\u0440\u043e\u0442\u0438\u0432\u043d\u0438\u043a",
            "\u0441\u0434\u0430\u043b",
            "\u0441\u0440\u0430\u0436",
            "\u043f\u0430\u0440\u0430\u0432\u043e\u0435\u043d",
            "\u0432\u043e\u0435\u043d\u043d",
            "\u0431\u0440\u0438\u0444\u0438\u043d\u0433",
            "\u0440\u0430\u0437\u0432\u0451\u0440\u0442",
        };
        var caseSensitiveTerms = new[]
        {
            "\u0421\u041e\u041f",
        };

        return caseInsensitiveTerms.Any(term =>
            question.Text.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            question.Answers.Any(answer => answer.Contains(term, StringComparison.OrdinalIgnoreCase))) ||
            caseSensitiveTerms.Any(term =>
                question.Text.Contains(term, StringComparison.Ordinal) ||
                question.Answers.Any(answer => answer.Contains(term, StringComparison.Ordinal)));
    }
}

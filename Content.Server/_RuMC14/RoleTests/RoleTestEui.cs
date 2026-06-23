using System.Linq;
using Content.Server.EUI;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._RuMC14.RoleTests;
using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RuMC14.RoleTests;

public sealed class RoleTestEui : BaseEui
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PlayTimeTrackingManager _playTime = default!;

    private ActiveRoleTest? _active;
    private Dictionary<string, int> _activeCorrectAnswers = new();
    private readonly string? _initialTestId;
    private string? _message;

    public RoleTestEui(string? initialTestId = null)
    {
        _initialTestId = initialTestId;
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        if (_initialTestId != null)
        {
            StartTest(_initialTestId);
            return;
        }

        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var playTimes = _playTime.GetTrackerTimes(Player);
        var retryCooldown = GetRetryCooldown(playTimes);
        var tests = EnumerateRoleTests()
            .OrderBy(test => test.Responsibility)
            .ThenBy(test => test.RequiresLaw)
            .ThenBy(test => test.Name)
            .Select(test =>
            {
                var questions = GetQuestions(test);
                var available = GetAvailableQuestions(test, questions).Count;
                return new RoleTestEntry(
                    test.Id,
                    test.Name,
                    test.Description,
                    test.Responsibility,
                    test.QuestionCount,
                    test.RequiresLaw,
                    playTimes.GetValueOrDefault(RoleTestShared.GetTracker(test.Id)) > TimeSpan.Zero,
                    retryCooldown == TimeSpan.Zero && CanStart(test, questions),
                    available);
            })
            .ToList();

        return new RoleTestEuiState(tests, _active, _message, retryCooldown);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case RoleTestEuiMsg.StartTest start:
                StartTest(start.TestId);
                break;
            case RoleTestEuiMsg.SubmitTest submit:
                SubmitTest(submit.TestId, submit.Answers);
                break;
            case RoleTestEuiMsg.CancelTest:
                _active = null;
                _message = null;
                StateDirty();
                break;
        }
    }

    private void StartTest(string testId)
    {
        var test = GetRoleTest(testId);
        if (test == null)
            return;

        var retryCooldown = GetRetryCooldown(_playTime.GetTrackerTimes(Player));
        if (retryCooldown > TimeSpan.Zero)
        {
            _message = GetRetryCooldownMessage(retryCooldown);
            StateDirty();
            return;
        }

        var allQuestions = GetQuestions(test);
        if (!CanStart(test, allQuestions))
        {
            _message = TryGetMissingQuestionPool(test, allQuestions, out var pool, out var available, out var required)
                ? Loc.GetString(
                    "role-test-not-enough-pool-questions",
                    ("pool", pool),
                    ("available", available),
                    ("required", required))
                : Loc.GetString(
                    "role-test-not-enough-questions",
                    ("available", GetAvailableQuestions(test, allQuestions).Count),
                    ("required", test.QuestionCount));
            StateDirty();
            return;
        }

        var selected = PickQuestions(test, allQuestions);
        var activeQuestions = BuildActiveQuestions(selected);
        _active = new ActiveRoleTest(
            test.Id,
            test.Name,
            activeQuestions);
        _message = null;
        StateDirty();
    }

    private void SubmitTest(string testId, Dictionary<string, int> answers)
    {
        if (_active == null || _active.TestId != testId)
        {
            return;
        }

        var correct = 0;
        foreach (var question in _active.Questions)
        {
            if (!_activeCorrectAnswers.TryGetValue(question.Id, out var correctAnswer))
                continue;

            if (answers.TryGetValue(question.Id, out var answer) &&
                answer == correctAnswer)
            {
                correct++;
            }
        }

        if (correct != _active.Questions.Count)
        {
            SetRetryCooldown();
            _message = Loc.GetString(
                "role-test-failed",
                ("correct", correct),
                ("total", _active.Questions.Count),
                ("minutes", (int) RoleTestShared.RetryCooldown.TotalMinutes));
            _active = null;
            _activeCorrectAnswers.Clear();
            StateDirty();
            return;
        }

        _playTime.AddTimeToTracker(Player, RoleTestShared.GetTracker(_active.TestId), TimeSpan.FromSeconds(1));
        _playTime.QueueSendTimers(Player);
        _message = Loc.GetString("role-test-passed", ("test", _active.Name));
        _active = null;
        _activeCorrectAnswers.Clear();
        StateDirty();
    }

    private void SetRetryCooldown()
    {
        var current = _playTime.GetPlayTimeForTracker(Player, RoleTestShared.RetryCooldownTracker);
        var retryAt = TimeSpan.FromTicks(DateTime.UtcNow.Add(RoleTestShared.RetryCooldown).Ticks);
        _playTime.AddTimeToTracker(Player, RoleTestShared.RetryCooldownTracker, retryAt - current);
        _playTime.QueueSendTimers(Player);
    }

    private static TimeSpan GetRetryCooldown(IReadOnlyDictionary<string, TimeSpan> playTimes)
    {
        var retryAt = playTimes.GetValueOrDefault(RoleTestShared.RetryCooldownTracker);
        var remaining = retryAt - TimeSpan.FromTicks(DateTime.UtcNow.Ticks);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private static string GetRetryCooldownMessage(TimeSpan remaining)
    {
        return Loc.GetString(
            "role-test-retry-cooldown",
            ("minutes", Math.Max(1, (int) Math.Ceiling(remaining.TotalMinutes))));
    }

    private bool CanStart(RoleTestDefinition test, List<TestQuestion> questions)
    {
        return !TryGetMissingQuestionPool(test, questions, out _, out _, out _);
    }

    private bool TryGetMissingQuestionPool(
        RoleTestDefinition test,
        List<TestQuestion> questions,
        out string pool,
        out int available,
        out int required)
    {
        var jobSpecificQuestions = GetJobSpecificQuestions(test, questions);
        if (jobSpecificQuestions.Count is > 0 and < RoleTestShared.RequiredJobSpecificQuestionCount)
        {
            pool = RoleTestShared.GetJobQuestionPool(test.JobId);
            available = jobSpecificQuestions.Count;
            required = RoleTestShared.RequiredJobSpecificQuestionCount;
            return true;
        }

        foreach (var (requiredPool, count) in test.RequiredPools)
        {
            var poolQuestions = questions
                .Where(question => question.Pools.Contains(requiredPool))
                .DistinctBy(question => NormalizeQuestionText(question.Text), StringComparer.OrdinalIgnoreCase)
                .Count();
            if (poolQuestions >= count)
                continue;

            pool = requiredPool;
            available = poolQuestions;
            required = count;
            return true;
        }

        pool = Loc.GetString("role-test-pool-total");
        available = GetAvailableQuestions(test, questions).Count;
        required = test.QuestionCount;
        return available < required;
    }

    private List<TestQuestion> GetAvailableQuestions(
        RoleTestDefinition test,
        List<TestQuestion> questions)
    {
        return questions
            .Where(question => question.Pools.Overlaps(test.QuestionPools))
            .DistinctBy(question => NormalizeQuestionText(question.Text), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<TestQuestion> PickQuestions(
        RoleTestDefinition test,
        List<TestQuestion> questions)
    {
        var selected = new List<TestQuestion>();
        var usedIds = new HashSet<string>();
        var usedTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var jobSpecificQuestions = GetJobSpecificQuestions(test, questions);
        _random.Shuffle(jobSpecificQuestions);
        AddQuestions(
            selected,
            usedIds,
            usedTexts,
            jobSpecificQuestions,
            Math.Min(RoleTestShared.RequiredJobSpecificQuestionCount, jobSpecificQuestions.Count));

        foreach (var (pool, count) in test.RequiredPools)
        {
            var poolQuestions = questions
                .Where(question => question.Pools.Contains(pool))
                .ToList();
            _random.Shuffle(poolQuestions);

            var missing = count - selected.Count(candidate => candidate.Pools.Contains(pool));
            AddQuestions(selected, usedIds, usedTexts, poolQuestions, Math.Max(0, missing));
        }

        var remaining = GetAvailableQuestions(test, questions)
            .Where(question => !usedIds.Contains(question.ID))
            .Where(question => !usedTexts.Contains(NormalizeQuestionText(question.Text)))
            .ToList();
        _random.Shuffle(remaining);
        selected.AddRange(remaining.Take(test.QuestionCount - selected.Count));
        _random.Shuffle(selected);
        return selected;
    }

    private static List<TestQuestion> GetJobSpecificQuestions(
        RoleTestDefinition test,
        IEnumerable<TestQuestion> questions)
    {
        return questions
            .Where(question => RoleTestShared.IsJobSpecificQuestion(question.ID, test.JobId))
            .DistinctBy(question => NormalizeQuestionText(question.Text), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddQuestions(
        List<TestQuestion> selected,
        HashSet<string> usedIds,
        HashSet<string> usedTexts,
        IEnumerable<TestQuestion> candidates,
        int count)
    {
        foreach (var question in candidates)
        {
            if (count == 0)
                return;

            if (!usedIds.Add(question.ID) ||
                !usedTexts.Add(NormalizeQuestionText(question.Text)))
            {
                continue;
            }

            selected.Add(question);
            count--;
        }
    }

    private static string NormalizeQuestionText(string text)
    {
        return string.Join(' ', text.Split((char[]) null!, StringSplitOptions.RemoveEmptyEntries));
    }

    private List<RoleTestQuestionData> BuildActiveQuestions(List<TestQuestion> selected)
    {
        _activeCorrectAnswers.Clear();
        var activeQuestions = new List<RoleTestQuestionData>();

        foreach (var question in selected)
        {
            var answers = question.Answers
                .Select((answer, index) => (Answer: answer, OriginalIndex: index))
                .ToList();
            _random.Shuffle(answers);

            var correctAnswer = answers.FindIndex(answer => answer.OriginalIndex == question.CorrectAnswer);
            _activeCorrectAnswers[question.ID] = correctAnswer;
            activeQuestions.Add(new RoleTestQuestionData(
                question.ID,
                question.Text,
                answers.Select(answer => answer.Answer).ToList()));
        }

        return activeQuestions;
    }

    private IEnumerable<RoleTestDefinition> EnumerateRoleTests()
    {
        foreach (var job in EnumeratePersonalizationJobs())
        {
            yield return CreateJobRoleTest(job);
        }
    }

    private RoleTestDefinition? GetRoleTest(string testId)
    {
        if (RoleTestShared.TryGetJobId(testId, out var jobId) &&
            _prototypes.TryIndex<JobPrototype>(jobId, out var job) &&
            IsTestedCmuJob(job))
        {
            return CreateJobRoleTest(job);
        }

        return null;
    }

    private IEnumerable<JobPrototype> EnumeratePersonalizationJobs()
    {
        var seen = new HashSet<string>();
        foreach (var department in _prototypes.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.EditorHidden)
                continue;

            foreach (var jobId in department.Roles)
            {
                if (!seen.Add(jobId.Id) || !_prototypes.TryIndex(jobId, out var job))
                    continue;

                if (!IsTestedCmuJob(job))
                    continue;

                yield return job;
            }
        }
    }

    private static bool IsPersonalizationJob(JobPrototype job)
    {
        return job.SetPreference &&
               !job.Hidden &&
               !RoleTestShared.IsRoleTestExempt(job);
    }

    private bool IsTestedCmuJob(JobPrototype job)
    {
        return IsPersonalizationJob(job) &&
               _prototypes.TryIndex<RoleTestQuestionPoolPrototype>(job.ID, out _);
    }

    private RoleTestDefinition CreateJobRoleTest(JobPrototype job)
    {
        var rolePool = _prototypes.Index<RoleTestQuestionPoolPrototype>(job.ID);
        var responsibility = rolePool.Responsibility;
        var requiresLaw = RoleTestShared.RequiresLaw(job);
        var configuredPools = rolePool.GetPools();
        configuredPools.Remove(RoleTestShared.CommonPool);
        configuredPools.Remove(RoleTestShared.LawPool);
        var questionPools = new HashSet<string>(configuredPools)
        {
            RoleTestShared.CommonPool,
        };
        var requiredPools = new Dictionary<string, int>
        {
            [RoleTestShared.CommonPool] = RoleTestShared.GetRequiredCommonQuestionCount(responsibility),
        };

        foreach (var configuredPool in configuredPools)
        {
            requiredPools[configuredPool] = RoleTestShared.GetRequiredConfiguredPoolQuestionCount(responsibility);
        }

        if (requiresLaw)
        {
            questionPools.Add(RoleTestShared.LawPool);
            requiredPools[RoleTestShared.LawPool] = RoleTestShared.GetRequiredLawQuestionCount(responsibility);
        }

        return new RoleTestDefinition(
            RoleTestShared.GetJobTestId(job.ID),
            Loc.GetString("role-test-job-name", ("job", job.LocalizedName)),
            job.LocalizedDescription,
            responsibility,
            RoleTestShared.GetQuestionCount(responsibility),
            questionPools,
            requiredPools,
            requiresLaw,
            job.ID,
            configuredPools);
    }

    private List<TestQuestion> GetQuestions(RoleTestDefinition test)
    {
        return _prototypes.EnumeratePrototypes<RoleTestQuestionPrototype>()
            .Select(question => new TestQuestion(
                question.ID,
                question.Text,
                question.Answers,
                question.CorrectAnswer,
                question.Pools))
            .Where(question => question.Pools.Overlaps(test.QuestionPools))
            .Where(question => IsQuestionEligibleForTest(question, test))
            .Where(question => !IsQuestionSpecificToAnotherJob(question.ID, test.JobId))
            .Where(question =>
                !question.Pools.Contains(RoleTestShared.CommonPool) ||
                RoleTestShared.IsGeneralCommonQuestion(question.ID))
            .ToList();
    }

    private bool IsQuestionSpecificToAnotherJob(string questionId, string jobId)
    {
        foreach (var pool in _prototypes.EnumeratePrototypes<RoleTestQuestionPoolPrototype>())
        {
            var otherJobId = pool.Job.Id;
            if (otherJobId == jobId)
                continue;

            if (RoleTestShared.IsJobSpecificQuestion(questionId, otherJobId))
                return true;
        }

        return false;
    }

    private static bool IsQuestionEligibleForTest(TestQuestion question, RoleTestDefinition test)
    {
        if (question.Pools.Contains(RoleTestShared.CommonPool))
            return true;

        if (RoleTestShared.IsJobSpecificQuestion(question.ID, test.JobId))
            return true;

        if (test.RequiresLaw && question.Pools.Contains(RoleTestShared.LawPool))
            return true;

        return question.Pools.Overlaps(test.ConfiguredPools);
    }

    private sealed record RoleTestDefinition(
        string Id,
        string Name,
        string? Description,
        RoleTestResponsibility Responsibility,
        int QuestionCount,
        HashSet<string> QuestionPools,
        Dictionary<string, int> RequiredPools,
        bool RequiresLaw,
        string JobId,
        HashSet<string> ConfiguredPools);

    private sealed record TestQuestion(
        string ID,
        string Text,
        List<string> Answers,
        int CorrectAnswer,
        HashSet<string> Pools);
}

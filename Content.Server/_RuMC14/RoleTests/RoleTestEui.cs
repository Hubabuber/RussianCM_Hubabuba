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
                    CanStart(test, questions),
                    available);
            })
            .ToList();

        return new RoleTestEuiState(tests, _active, _message);
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
            _message = Loc.GetString(
                "role-test-failed",
                ("correct", correct),
                ("total", _active.Questions.Count));
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
        foreach (var (requiredPool, count) in test.RequiredPools)
        {
            var poolQuestions = questions.Count(question => question.Pools.Contains(requiredPool));
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
            .ToList();
    }

    private List<TestQuestion> PickQuestions(
        RoleTestDefinition test,
        List<TestQuestion> questions)
    {
        var selected = new List<TestQuestion>();
        var used = new HashSet<string>();

        foreach (var (pool, count) in test.RequiredPools)
        {
            var poolQuestions = questions
                .Where(question => question.Pools.Contains(pool))
                .Where(question => used.Add(question.ID))
                .ToList();
            _random.Shuffle(poolQuestions);
            selected.AddRange(poolQuestions.Take(count));
        }

        var remaining = GetAvailableQuestions(test, questions)
            .Where(question => !used.Contains(question.ID))
            .ToList();
        _random.Shuffle(remaining);
        selected.AddRange(remaining.Take(test.QuestionCount - selected.Count));
        _random.Shuffle(selected);
        return selected;
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
            IsPersonalizationJob(job))
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

                if (!IsPersonalizationJob(job))
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

    private RoleTestDefinition CreateJobRoleTest(JobPrototype job)
    {
        var responsibility = RoleTestShared.GetResponsibility(job);
        var requiresLaw = RoleTestShared.RequiresLaw(job);
        var rolePool = GetRoleQuestionPool(job);
        var questionPools = new HashSet<string>
        {
            RoleTestShared.CommonPool,
            rolePool,
        };
        var requiredPools = new Dictionary<string, int>
        {
            [RoleTestShared.CommonPool] = RoleTestShared.GetRequiredCommonQuestionCount(responsibility),
            [rolePool] = RoleTestShared.GetRequiredRoleQuestionCount(responsibility, requiresLaw),
        };

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
            requiresLaw);
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
            .ToList();
    }

    private string GetRoleQuestionPool(JobPrototype job)
    {
        return _prototypes.TryIndex<RoleTestQuestionPoolPrototype>(job.ID, out var pool)
            ? pool.Pool
            : RoleTestShared.GetJobQuestionPool(job.ID);
    }

    private sealed record RoleTestDefinition(
        string Id,
        string Name,
        string? Description,
        RoleTestResponsibility Responsibility,
        int QuestionCount,
        HashSet<string> QuestionPools,
        Dictionary<string, int> RequiredPools,
        bool RequiresLaw);

    private sealed record TestQuestion(
        string ID,
        string Text,
        List<string> Answers,
        int CorrectAnswer,
        HashSet<string> Pools);
}

using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._RuMC14.RoleTests;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Client._RuMC14.RoleTests;

public sealed partial class RoleTestWindow : DefaultWindow
{
    private static readonly string[] AnswerPrefixes = { "A", "B", "C", "D" };

    private readonly BoxContainer _content;
    private readonly Dictionary<string, int> _answers = new();

    public event Action<string>? StartTest;
    public event Action<string, Dictionary<string, int>>? SubmitTest;
    public event Action? CancelTest;

    public RoleTestWindow()
    {
        Title = Loc.GetString("role-test-window-title");
        MinSize = new Vector2(720, 640);

        _content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(8),
        };

        Contents.AddChild(new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { _content },
        });
    }

    public void SetState(RoleTestEuiState state)
    {
        _content.DisposeAllChildren();
        _answers.Clear();

        if (!string.IsNullOrWhiteSpace(state.Message))
        {
            _content.AddChild(new Label
            {
                Text = state.Message,
                StyleClasses = { StyleNano.StyleClassCrtText },
            });
        }

        if (state.Active != null)
        {
            BuildActiveTest(state.Active);
            return;
        }

        if (state.RetryCooldown > TimeSpan.Zero)
        {
            _content.AddChild(new Label
            {
                Text = Loc.GetString(
                    "role-test-retry-cooldown",
                    ("minutes", Math.Max(1, (int) Math.Ceiling(state.RetryCooldown.TotalMinutes)))),
                StyleClasses = { StyleNano.StyleClassCrtText },
            });
        }

        BuildTestList(state.Tests);
    }

    private void BuildTestList(List<RoleTestEntry> tests)
    {
        foreach (var test in tests)
        {
            var panel = new PanelContainer
            {
                HorizontalExpand = true,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        Margin = new Thickness(8),
                        Children =
                        {
                            new Label
                            {
                                Text = test.Name,
                                StyleClasses = { StyleBase.StyleClassLabelHeading },
                            },
                            new Label
                            {
                                Text = GetTestSummary(test),
                                StyleClasses = { StyleBase.StyleClassLabelSubText },
                            },
                            MakeStartButton(test),
                        },
                    },
                },
            };

            _content.AddChild(panel);
        }
    }

    private Button MakeStartButton(RoleTestEntry test)
    {
        var button = new Button
        {
            Text = test.Passed
                ? Loc.GetString("role-test-status-passed")
                : Loc.GetString("role-test-start-button"),
            Disabled = test.Passed || !test.CanStart,
            HorizontalAlignment = HAlignment.Left,
        };

        button.OnPressed += _ => StartTest?.Invoke(test.Id);
        return button;
    }

    private static string GetTestSummary(RoleTestEntry test)
    {
        var responsibility = test.Responsibility switch
        {
            RoleTestResponsibility.Low => Loc.GetString("role-test-responsibility-low"),
            RoleTestResponsibility.Medium => Loc.GetString("role-test-responsibility-medium"),
            RoleTestResponsibility.High => Loc.GetString("role-test-responsibility-high"),
            _ => test.Responsibility.ToString(),
        };

        var law = test.RequiresLaw
            ? Loc.GetString("role-test-summary-law")
            : Loc.GetString("role-test-summary-no-law");

        return Loc.GetString(
            "role-test-summary",
            ("responsibility", responsibility),
            ("count", test.QuestionCount),
            ("law", law),
            ("available", test.AvailableQuestions));
    }

    private void BuildActiveTest(ActiveRoleTest active)
    {
        _content.AddChild(new Label
        {
            Text = active.Name,
            StyleClasses = { StyleBase.StyleClassLabelHeading },
        });

        Button? submit = null;
        void UpdateSubmit()
        {
            if (submit != null)
                submit.Disabled = _answers.Count < active.Questions.Count;
        }

        for (var questionIndex = 0; questionIndex < active.Questions.Count; questionIndex++)
        {
            var question = active.Questions[questionIndex];
            var questionPanel = new PanelContainer
            {
                HorizontalExpand = true,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        SeparationOverride = 6,
                        Margin = new Thickness(8),
                        Children =
                        {
                            new Label
                            {
                                Text = Loc.GetString(
                                    "role-test-question-title",
                                    ("number", questionIndex + 1),
                                    ("total", active.Questions.Count)),
                                StyleClasses = { StyleBase.StyleClassLabelSubText },
                            },
                            new Label
                            {
                                Text = question.Text,
                                StyleClasses = { StyleNano.StyleClassCrtText },
                            },
                            BuildAnswerButtons(question, UpdateSubmit),
                        },
                    },
                },
            };

            _content.AddChild(questionPanel);
        }

        var controls = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };

        submit = new Button
        {
            Text = Loc.GetString("role-test-submit-button"),
            Disabled = true,
            StyleClasses = { StyleNano.StyleClassCrtAttentionButton },
        };
        submit.OnPressed += _ => SubmitTest?.Invoke(active.TestId, new Dictionary<string, int>(_answers));

        var cancel = new Button
        {
            Text = Loc.GetString("role-test-cancel-button"),
            StyleClasses = { StyleNano.StyleClassCrtButton },
        };
        cancel.OnPressed += _ => CancelTest?.Invoke();

        controls.AddChild(submit);
        controls.AddChild(cancel);
        _content.AddChild(controls);
    }

    private BoxContainer BuildAnswerButtons(RoleTestQuestionData question, Action onAnswered)
    {
        var controls = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };

        var buttons = new List<Button>();

        for (var i = 0; i < question.Answers.Count; i++)
        {
            var answer = i;
            var prefix = i < AnswerPrefixes.Length ? $"{AnswerPrefixes[i]}. " : string.Empty;
            var button = new Button
            {
                Text = $"{prefix}{question.Answers[i]}",
                HorizontalExpand = true,
                StyleClasses = { StyleNano.StyleClassCrtButton },
            };

            button.OnPressed += _ =>
            {
                _answers[question.Id] = answer;

                foreach (var answerButton in buttons)
                {
                    answerButton.RemoveStyleClass(StyleNano.StyleClassCrtAttentionButton);
                    answerButton.AddStyleClass(StyleNano.StyleClassCrtButton);
                }

                button.RemoveStyleClass(StyleNano.StyleClassCrtButton);
                button.AddStyleClass(StyleNano.StyleClassCrtAttentionButton);
                onAnswered();
            };

            buttons.Add(button);
            controls.AddChild(button);
        }

        return controls;
    }
}

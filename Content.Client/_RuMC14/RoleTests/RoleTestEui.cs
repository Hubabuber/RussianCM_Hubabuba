using Content.Client.Eui;
using Content.Shared._RuMC14.RoleTests;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._RuMC14.RoleTests;

[UsedImplicitly]
public sealed class RoleTestEui : BaseEui
{
    private RoleTestWindow? _window;

    public override void Opened()
    {
        base.Opened();

        _window = new RoleTestWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.StartTest += id => SendMessage(new RoleTestEuiMsg.StartTest(id));
        _window.SubmitTest += (id, answers) => SendMessage(new RoleTestEuiMsg.SubmitTest(id, answers));
        _window.CancelTest += () => SendMessage(new RoleTestEuiMsg.CancelTest());
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Close();
        _window = null;
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is RoleTestEuiState roleTestState)
            _window?.SetState(roleTestState);
    }
}

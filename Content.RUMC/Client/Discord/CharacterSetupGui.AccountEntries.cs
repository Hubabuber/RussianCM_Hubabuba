using Content.Client._RuCM.Kaban;
using Content.Client._RuCM.Qualifications;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Lobby.UI;

public sealed partial class CharacterSetupGui
{
    private void InitializeRuCMAccountEntries()
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        entities.System<QualificationEntrySystem>().AttachPersonalization(AccountActions);
        entities.System<KabanEntrySystem>().AttachPersonalization(AccountActions);
    }
}

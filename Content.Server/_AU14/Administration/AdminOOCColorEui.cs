using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._AU14.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Server._AU14.Administration;

public sealed class AdminOOCColorEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private readonly ISawmill _sawmill;
    private readonly List<Content.Server.Database.AdminRank> _ranks = new();
    private bool _isLoading;

    public AdminOOCColorEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("admin.ooc-color");
    }

    public override void Opened()
    {
        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;
        LoadFromDb();
    }

    public override void Closed()
    {
        _adminManager.OnPermsChanged -= OnPermsChanged;
        base.Closed();
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminOOCColorEuiState
        {
            IsLoading = _isLoading,
            Ranks = _ranks
                .OrderBy(rank => rank.Name)
                .ThenBy(rank => rank.Id)
                .Select(rank => new AdminOOCColorRank
                {
                    Id = rank.Id,
                    Name = rank.Name,
                    Color = rank.OOCColor,
                })
                .ToList(),
        };
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SetAdminRankOOCColor update || IsShutDown || !IsHost())
            return;

        var rank = await _db.GetAdminRankAsync(update.RankId);
        if (rank == null)
        {
            _sawmill.Warning("{Player} tried to set OOC color for missing admin rank {RankId}.", Player, update.RankId);
            return;
        }

        if (!IsHost())
        {
            Close();
            return;
        }

        string? normalizedColor = null;
        if (!string.IsNullOrWhiteSpace(update.Color))
        {
            if (Color.TryFromHex(update.Color.Trim(), out var color) == false)
            {
                _sawmill.Warning("{Player} tried to set invalid OOC color {Color} for admin rank {RankId}.", Player, update.Color, update.RankId);
                return;
            }

            normalizedColor = color.ToHex();
        }

        rank.OOCColor = normalizedColor;
        await _db.UpdateAdminRankAsync(rank);
        _adminManager.ReloadAdminsWithRank(rank.Id);
        _sawmill.Info("{Player} set OOC color for admin rank {Rank}: {Color}.", Player, rank.Name, normalizedColor ?? "clear");

        LoadFromDb();
    }

    private async void LoadFromDb()
    {
        if (!IsHost())
        {
            Close();
            return;
        }

        _isLoading = true;
        StateDirty();

        var (_, ranks) = await _db.GetAllAdminAndRanksAsync();
        if (IsShutDown)
            return;

        _ranks.Clear();
        _ranks.AddRange(ranks);
        _isLoading = false;
        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !IsHost())
            Close();
    }

    private bool IsHost()
    {
        return _adminManager.HasAdminFlag(Player, AdminFlags.Host);
    }
}

using System.Data.Common;
using Content.Server.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace Content.DiscordBot.Tests;

[TestFixture]
public sealed class RMCPatronPersistenceTests
{
    private SqliteConnection _connection = default!;
    private DbContextOptions<SqliteServerDbContext> _options = default!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await using var command = _connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE rmc_patrons (
                player_id TEXT NOT NULL PRIMARY KEY,
                tier_id INTEGER NOT NULL,
                ghost_color INTEGER NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
        _options = new DbContextOptionsBuilder<SqliteServerDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    [TearDown]
    public async Task TearDown()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task SetTierInsertsMissingPatron()
    {
        var playerId = Guid.NewGuid();
        await using var db = new SqliteServerDbContext(_options);

        Assert.That(await RMCPatronPersistence.SetTierAsync(db, playerId, 3), Is.True);

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT tier_id FROM rmc_patrons WHERE player_id = $playerId";
        command.Parameters.AddWithValue("$playerId", playerId);
        Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(3));
    }

    [Test]
    public async Task SetTierReturnsFalseWhenTierIsUnchanged()
    {
        var playerId = Guid.NewGuid();
        await using var db = new SqliteServerDbContext(_options);

        Assert.That(await RMCPatronPersistence.SetTierAsync(db, playerId, 3), Is.True);
        Assert.That(await RMCPatronPersistence.SetTierAsync(db, playerId, 3), Is.False);

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM rmc_patrons";
        Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(1L));
    }

    [Test]
    public async Task SetTierConcurrentlyInsertsMissingPatronOnce()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"rmc-patron-{Guid.NewGuid():N}.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            DefaultTimeout = 10,
            Pooling = false,
        }.ToString();

        try
        {
            await using (var setupConnection = new SqliteConnection(connectionString))
            {
                await setupConnection.OpenAsync();
                await using var setupCommand = setupConnection.CreateCommand();
                setupCommand.CommandText = """
                    CREATE TABLE rmc_patrons (
                        player_id TEXT NOT NULL PRIMARY KEY,
                        tier_id INTEGER NOT NULL,
                        ghost_color INTEGER NULL
                    );
                    """;
                await setupCommand.ExecuteNonQueryAsync();
            }

            var insertBarrier = new ConcurrentPatronInsertBarrier();
            await using var firstConnection = new SqliteConnection(connectionString);
            await using var secondConnection = new SqliteConnection(connectionString);
            await firstConnection.OpenAsync();
            await secondConnection.OpenAsync();

            var firstOptions = new DbContextOptionsBuilder<SqliteServerDbContext>()
                .UseSqlite(firstConnection)
                .AddInterceptors(insertBarrier)
                .Options;
            var secondOptions = new DbContextOptionsBuilder<SqliteServerDbContext>()
                .UseSqlite(secondConnection)
                .AddInterceptors(insertBarrier)
                .Options;

            await using var firstDb = new SqliteServerDbContext(firstOptions);
            await using var secondDb = new SqliteServerDbContext(secondOptions);
            var playerId = Guid.NewGuid();
            var firstSet = RMCPatronPersistence.SetTierAsync(firstDb, playerId, 3);
            var secondSet = RMCPatronPersistence.SetTierAsync(secondDb, playerId, 3);
            var results = Array.Empty<bool>();

            Assert.DoesNotThrowAsync(async () =>
            {
                results = await Task.WhenAll(firstSet, secondSet);
            });
            Assert.That(results, Is.EquivalentTo(new[] { true, false }));

            await using var countCommand = firstConnection.CreateCommand();
            countCommand.CommandText = "SELECT count(*) FROM rmc_patrons WHERE player_id = $playerId";
            countCommand.Parameters.AddWithValue("$playerId", playerId);
            Assert.That(await countCommand.ExecuteScalarAsync(), Is.EqualTo(1L));
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Test]
    public async Task SetTierUpdatesExistingPatronWithoutAddingRow()
    {
        var playerId = Guid.NewGuid();
        await using var db = new SqliteServerDbContext(_options);

        Assert.That(await RMCPatronPersistence.SetTierAsync(db, playerId, 3), Is.True);
        Assert.That(await RMCPatronPersistence.SetTierAsync(db, playerId, 7), Is.True);

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT count(*), max(tier_id) FROM rmc_patrons";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.That(await reader.ReadAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetInt32(0), Is.EqualTo(1));
            Assert.That(reader.GetInt32(1), Is.EqualTo(7));
        });
    }

    [Test]
    public async Task RemoveReturnsFalseWhenPatronDoesNotExist()
    {
        await using var db = new SqliteServerDbContext(_options);

        Assert.That(await RMCPatronPersistence.RemoveAsync(db, Guid.NewGuid()), Is.False);
    }

    [Test]
    public async Task RemoveDeletesExistingPatron()
    {
        var playerId = Guid.NewGuid();
        await using var db = new SqliteServerDbContext(_options);

        Assert.That(await RMCPatronPersistence.SetTierAsync(db, playerId, 3), Is.True);
        Assert.That(await RMCPatronPersistence.RemoveAsync(db, playerId), Is.True);

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM rmc_patrons";
        Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(0L));
    }

    private sealed class ConcurrentPatronInsertBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _bothInsertsReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _insertCount;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await WaitForBothInsertsAsync(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await WaitForBothInsertsAsync(command, cancellationToken);
            return result;
        }

        private async Task WaitForBothInsertsAsync(DbCommand command, CancellationToken cancellationToken)
        {
            var sql = command.CommandText.TrimStart();
            if (!sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                !sql.Contains("rmc_patrons", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Interlocked.Increment(ref _insertCount) == 2)
                _bothInsertsReached.TrySetResult();

            await _bothInsertsReached.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
}

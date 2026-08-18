using System.Reflection;
using Content.Server.Database;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Server.Database;

[TestFixture]
public sealed class DonorCapeProfilePersistenceTest
{
    [Test]
    public void SelectedDonorCapeSurvivesDatabaseRoundTrip()
    {
        var selectedCape = new ProtoId<RMCDonorCapePrototype>("RMCDonorCape01");
        var original = HumanoidCharacterProfile.DefaultWithSpecies()
            .WithSelectedDonorCape(selectedCape);

        var serialize = typeof(ServerDbBase).GetMethod(
            "ConvertProfiles",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(HumanoidCharacterProfile), typeof(int), typeof(Profile) },
            null);
        Assert.That(serialize, Is.Not.Null);

        var stored = (Profile) serialize!.Invoke(null, new object[] { original, 0, null! })!;

        var deserialize = typeof(ServerDbBase).GetMethod(
            "ConvertProfiles",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Profile) },
            null);
        Assert.That(deserialize, Is.Not.Null);

        var restored = (HumanoidCharacterProfile) deserialize!.Invoke(null, new object[] { stored })!;

        Assert.That(restored.SelectedDonorCape, Is.EqualTo(selectedCape));
    }
}

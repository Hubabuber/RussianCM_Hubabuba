using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Stamina;
using Content.Shared.Inventory;
using NUnit.Framework;

namespace Content.Tests.Shared._RMC14.Stamina;

[TestFixture]
public sealed class RMCStaminaArmorTest
{
    [Test]
    public void OuterBodyArmorBlocksStaminaDamage()
    {
        var armor = new CMGetArmorEvent(SlotFlags.OUTERCLOTHING, Bullet: 20);

        Assert.That(RMCStaminaSystem.ArmorBlocksStaminaDamage(armor), Is.True);
    }

    [Test]
    public void XenoArmorDoesNotCountAsBodyArmor()
    {
        var armor = new CMGetArmorEvent(SlotFlags.OUTERCLOTHING, XenoArmor: 55);

        Assert.That(RMCStaminaSystem.ArmorBlocksStaminaDamage(armor), Is.False);
    }
}

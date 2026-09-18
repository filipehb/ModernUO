using ModernUO.Serialization;
using Server.Engines.Cosmere;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CosmereMetalVial : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CosmereMetalFlags _metals;

    [Constructible]
    public CosmereMetalVial() : this(CosmereMetalFlags.AllStarter)
    {
    }

    [Constructible]
    public CosmereMetalVial(CosmereMetalFlags metals) : base(0xF0E)
    {
        _metals = metals;
        Weight = 1.0;
        Stackable = true;
        Name = metals == CosmereMetalFlags.AllStarter ? "metal vial (mixed)" : "metal vial";
        Hue = 0x482;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm || !IsChildOf(from.Backpack) && !IsChildOf(from))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (!CosmereAllomancy.TryDrink(pm, _metals, consumeItem: true))
        {
            return;
        }

        Consume();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Metals"}\t{_metals}");
    }
}

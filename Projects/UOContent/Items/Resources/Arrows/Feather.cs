using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Feather : Item, ICommodity
{
    [Constructible]
    public Feather(int amount = 1) : base(0x1BD1)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

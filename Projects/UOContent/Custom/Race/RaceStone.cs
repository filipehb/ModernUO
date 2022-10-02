using Server.Gumps;

namespace Server.Custom.Race;

public class RaceStone : Item
{
    [Constructible]
    public RaceStone() : base(0xED4)
    {
        Weight = 1.0;
        Movable = false;
        Hue = 0x2D1;
    }

    public override bool Decays => false;

    public override string DefaultName => "Pedra de seleção da raça";

    public override void OnDoubleClick(Mobile from)
    {
        from.Frozen = true;

        if (from.HasGump<RaceGump>())
        {
            from.CloseGump<RaceGump>();
        }

        from.SendGump(new RaceGump(from));
    }

    public RaceStone(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        switch (version)
        {
            case 0:
                {
                    break;
                }
        }
    }
}

using Server.Custom.Race;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class RaceGump : Gump
{
    public RaceGump(Mobile from) : base(0, 0)
    {
        Closable = false;
        Disposable = false;
        Draggable = false;
        Resizable = false;

        AddPage(0);

        AddBackground(143, 117, 496, 336, 9200);

        AddLabel(263, 154, 1280, @"Seleção de raça");
        AddLabel(248, 193, 0, @"Humano");
        AddLabel(251, 240, 0, @"Elfo");
        AddLabel(251, 281, 0, @"Anão");
        //Removido no momento
        //AddLabel(251, 365, 0, @"Hobbit");
        //AddLabel(252, 322, 0, @"Orc");

        AddButton(225, 240, 209, 208, (int)Races.Humano, GumpButtonType.Reply, 0);
        AddButton(225, 281, 209, 208, (int)Races.Elfo, GumpButtonType.Reply, 0);
        AddButton(225, 194, 209, 208, (int)Races.Anao, GumpButtonType.Reply, 0);
        // AddButton(226, 322, 209, 208, (int) RacesCustom.Hobbit, GumpButtonType.Reply, 0);
        //AddButton(225, 364, 209, 208, (int) Races.Orc, GumpButtonType.Reply, 0);

        AddImage(604, 127, 203);
        AddImage(138, 128, 202);
        AddImage(178, 107, 201);
        AddImage(179, 416, 233);
        AddImage(138, 416, 204);
        AddImage(138, 107, 206);
        AddImage(605, 107, 207);
        AddImage(316, 177, 1418);
        AddImage(604, 416, 205);
    }

    public override void OnResponse(NetState state, RelayInfo info)
    {
        Mobile m = state.Mobile;

        PlayerMobile pm = m as PlayerMobile;
        if (pm == null)
            return;

        pm.Race = Race.Human;
        pm.Hunger = 40;

        switch (info.ButtonID)
        {
            case (int)Races.Humano:
                {
                    // Neste local tem os hues e bodies e outras coisas Projects/UOContent/Misc/RaceDefinitions.cs
                    // Aqui tambem tem algumas coisas boas Projects/UOContent/Engines/ML Quests/Gumps/RaceChangeGump.cs
                    //movetoworld 3031 2790 - cidade humana
                    RaceSave.SetRace(pm, Races.Humano, false);
                    m.SendGump(new SkinGump(pm));
                    // m.SendGump(new FormationKitGump(pm, Races.Humano));
                    break;
                }
            case (int)Races.Elfo:
                {
                    RaceSave.SetRace(pm, Races.Elfo, false);
                    m.SendGump(new SkinGump(pm));
                    // go 2666 2154 - talvez cidade elfica
                    // m.SendGump(new FormationKitGump(pm, Races.Elfo));
                    break;
                }
            case (int)Races.Anao:
                {
                    RaceSave.SetRace(pm, Races.Anao, false);
                    m.SendGump(new SkinGump(pm));
                    // go 2551 1335 - cidade anã
                    // m.SendGump(new FormationKitGump(pm, Races.Anao));
                    break;
                }
            case (int)Races.Hobbit:
                {
                    // m.SendGump(new FormationKitGump(pm, RacesCustom.Hobbit));
                    break;
                }
            case (int)Races.Orc:
                {
                    // m.SendGump(new FormationKitGump(pm, RacesCustom.Orc));
                    break;
                }
        }
    }
}

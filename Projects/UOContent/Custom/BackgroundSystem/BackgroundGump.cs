using System;
using Server.Gumps;
using Server.Network;

namespace Server.Custom.BackgroundSystem;

public class BackgroundGump : Gump
{
    public BackgroundGump() : base(50, 50)
    {
        AddBackground(0, 0, 400, 500, 5054);

        AddLabel(125, 10, 1152, "Escolha seu Histórico:");

        int y = 70;
        int buttonID = 1;

        var backgrounds = Enum.GetNames(typeof(Backgrounds));

        foreach(string background in backgrounds)
        {
            AddButton(50, y, 4005, 4007, buttonID);
            AddLabel(80, y, 0, background);
            y += 30;
            buttonID++;
        }
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        Mobile m = sender.Mobile;
        int buttonID = info.ButtonID;
        // Lidar com a resposta
    }
}

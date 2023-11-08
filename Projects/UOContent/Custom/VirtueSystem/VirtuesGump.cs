using System;
using System.Linq;
using Server.Gumps;
using Server.Network;

namespace Server.Custom.VirtueSystem;

public class VirtuesGump : Gump
{
    public VirtuesGump() : base(50, 50)
    {
        this.AddBackground(0, 0, 400, 500, 5054);

        this.AddLabel(125, 10, 1152, "Escolha sua Virtude Cultural:");

        int y = 70;
        int buttonID = 1;

        var virtues = Enum.GetNames(typeof(Virtues));

        foreach(string virtue in virtues)
        {
            this.AddButton(50, y, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            this.AddLabel(80, y, 0, virtue);
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

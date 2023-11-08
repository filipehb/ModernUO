using System;
using System.Linq;
using Server.Gumps;
using Server.Network;

namespace Server.Custom.Classes;

public class ClassesGump : Gump
{
    public ClassesGump() : base(50, 50)
    {
        var classes = Enum.GetNames(typeof(Classes)).Skip(1);

        AddBackground(0, 0, 400, 300, 5054);

        AddLabel(125, 10, 1152, "Escolha sua Classe:");

        int y = 70;
        int buttonID = 1;

        foreach(string classe in classes)
        {
            AddButton(50, y, 4005, 4007, buttonID);
            AddLabel(80, y, 0, classe);
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

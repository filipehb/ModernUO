using System;
using Server.Custom.Commands;
using Server.Logging;
using Server.Network;

namespace Server.Gumps;

public class SkinGump : Gump
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SkinTone));

    private static int[] hues =
    {
        1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020,
        1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 1030, 1031, 1032, 1033, 1034, 1035, 1036, 1037, 1038, 1039,
        1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055, 1056, 1057, 1058
    };

    private static bool Allow_Preview = true; //Set to false if you don't want to let players preview colors.

    private static string Skin_Tone_Message =
        @"Sua pele vai mudar de cor durante um tempo e logo devo retornar a cor anterior";

    private static string Top_Right_Message =
        @"Você pode escolher sua cor aqui, mas será definitiva!";

    private static int Start_X = 15, Start_Y = 20;

    //End Settings, do not edit below unless you know what you're doing!


    private Mobile caller;
    private int Last_Selected;

    public SkinGump(Mobile from, int lastselected)
        : base(Start_X, Start_Y)
    {
        Show_The_Gump(from, lastselected);
    }

    public SkinGump(Mobile from)
        : base(Start_X, Start_Y)
    {
        Show_The_Gump(from, Last_Selected);
    }


    private void Show_The_Gump(Mobile from, int lastselected)
    {
        Last_Selected = lastselected;
        caller = from;
        if (caller == null)
        {
            return;
        }

        try
        {
            Closable = false;
            Disposable = false;
            Draggable = false;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 1, 472, 572, 9300);
            AddPage(1);

            int Race_Pic_X = -15, Race_Pic_Y = -15;

            /*if (caller.Race == Race.Human)*/
            AddImage(
                Race_Pic_X,
                Race_Pic_Y,
                1888,
                Last_Selected >= 0 ? Last_Selected < hues.Length ? hues[Last_Selected] : hues[0] : hues[0]
            );
            //if (caller.Race == Race.Elf) AddImage(Race_Pic_X, Race_Pic_Y, 1893, Last_Selected >= 0 ? Last_Selected < hues.Length ? hues[Last_Selected] : hues[0] : hues[0]);
            //if (caller.Race == Race.Gargoyle) AddImage(Race_Pic_X, Race_Pic_Y, 665, Last_Selected >= 0 ? Last_Selected < hues.Length ? hues[Last_Selected] : hues[0] : hues[0]);

            Show_Top_Right_Message();

            AddButton(320, 549, 2445, 2445, 1000, GumpButtonType.Reply, 1000); //Apply
            AddLabel(360, 549, 42, "Aplicar");

            if (Allow_Preview) AddButton(200, 549, 2445, 2445, 1001, GumpButtonType.Reply, 1001); //Preview
            if (Allow_Preview) AddLabel(234, 549, 42, "Preview");

            int max_height = 7, max_width = 7;

            for (int ii = 0; ii < max_width; ii++)
                for (int i = 0; i + (ii * max_height) < hues.Length && i < max_height; i++)
                {
                    AddRadio(
                        10 + (ii * 75),
                        260 + (i * 40),
                        2151,
                        2154,
                        i + (ii * max_height) == Last_Selected ? true : false,
                        (i) + (ii * max_height)
                    );
                    AddImage(43 + (ii * 75), 260 + (i * 40), 1227, hues[(i) + (ii * max_height)]);
                }
        }
        catch (Exception e)
        {
            logger.Error($"SkinChangeGump: Error 3: {e.Message} Caller: {caller.Account}");
        }
    }

    private void Show_Top_Right_Message()
    {
        int Max_Length = 31;
        int Line_Height = 16;
        int tempnum, tempnum2;

        for (int i = 0; i <= (Top_Right_Message.Length / Max_Length); i++)
        {
            tempnum = i * Max_Length;
            tempnum2 = Top_Right_Message.Length - tempnum;
            if (tempnum < Top_Right_Message.Length)
                AddLabel(
                    266,
                    5 + (Line_Height * i),
                    38,
                    Top_Right_Message.Substring(
                        tempnum,
                        (tempnum + Max_Length) > Top_Right_Message.Length ? tempnum2 : Max_Length
                    )
                );
        }
    }


    public override void OnResponse(NetState sender, RelayInfo info)
    {
        try
        {
            Mobile from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 0:
                    {
                        if (from.HasGump<SkinGump>())
                            from.CloseGump<SkinGump>();
                        return;
                    }
                case 1000:
                    {
                        for (int i = 0; i < hues.Length; i++)
                            if (info.IsSwitched(i))
                            {
                                from.Hue = hues[i];
                                break;
                            }

                        break;
                    }
                case 1001:
                    {
                        if (Allow_Preview)
                            for (int i = 0; i < hues.Length; i++)
                                if (info.IsSwitched(i))
                                {
                                    new PreviewColor(hues[i], from, i);
                                    from.SendMessage(38, Skin_Tone_Message);
                                    break;
                                }

                        break;
                    }
            }
        }
        catch (Exception e)
        {
            logger.Error($"SkinChangeGump: Error 4: {e.Message} Caller: {caller.Account}");
        }
    }
}

public class PreviewColor
{
    private int oldhue, PAGE;
    private Mobile from;

    public PreviewColor(int hue, Mobile who, int page)
    {
        from = who;
        oldhue = who.Hue;
        who.Hue = hue;
        PAGE = page;
        Start_Timer(TimeSpan.FromSeconds(5));
    }

    public void Start_Timer(TimeSpan s)
    {
        Timer.DelayCall(s, BackToNormal);
    }

    public void BackToNormal()
    {
        from.Hue = oldhue;
        if (from.HasGump<SkinGump>())
            from.CloseGump<SkinGump>();
        from.SendGump(new SkinGump(from, PAGE));
    }
}

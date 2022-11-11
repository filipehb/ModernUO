using Server.Gumps;
using Server.Logging;
using Server.Targeting;

namespace Server.Custom.Commands;

public class SkinTone
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SkinTone));


    public static void Initialize()
    {
        CommandSystem.Register("MudarCor", AccessLevel.Counselor, SkinChange_OnCommand);
        CommandSystem.Register("MudaCor", AccessLevel.Counselor, SkinChange_OnCommand);
        CommandSystem.Register("mudarcor", AccessLevel.Counselor, SkinChange_OnCommand);
        CommandSystem.Register("mudacor", AccessLevel.Counselor, SkinChange_OnCommand);
    }

    [Usage("MudarCor ou MudaCor ou mudarcor ou mudacor")]
    [Description("Este comando vai enviar o gump para ajustar a cor de um alvo")]
    public static void SkinChange_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile != null)
        {
            e.Mobile.SendMessage("Selecione o alvo que você quer que ajuste sua cor");
            e.Mobile.Target = new MobileTarget();
        }
    }

    private class MobileTarget : Target
    {
        public MobileTarget() : base(15, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targ)
        {
            if (targ is Item)
            {
                from.SendMessage("Você só pode selecionar Mobiles.");
                return;
            }

            logger.Information("");

            if (targ is Mobile m)
            {
                m.SendGump(new SkinGump(m));
            }
        }
    }
}

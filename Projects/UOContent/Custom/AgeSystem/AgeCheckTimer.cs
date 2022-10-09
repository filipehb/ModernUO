using System;
using Server.Custom.FaintSystem;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.AgeSystem;

public class AgeCheckTimer : Timer
{
    public AgeCheckTimer() : base(TimeSpan.FromHours(24), TimeSpan.FromHours(24))
    {
    }

    public static void Initialize()
    {
        new AgeCheckTimer().Start();
    }

    protected override void OnTick()
    {
        CheckAge();
    }

    private void CheckAge()
    {
        var isGoingToDie = Utility.RandomBool();
        foreach (NetState state in TcpServer.Instances)
        {
            if (state.Mobile == null)
            {
                continue;
            }

            if (state.Mobile.AccessLevel == AccessLevel.Player && state.Mobile.Alive && isGoingToDie &&
                AgeUtils.IsLifeLimit(state.Mobile as PlayerMobile))
            {
                PlayerMobile playerMobile = state.Mobile as PlayerMobile;
                if (playerMobile != null)
                {
                    playerMobile.SendMessage(38, "Seu tempo por essas terras chegou ao fim");
                    playerMobile.SendMessage(38, AgeUtils.GenerateDeath());
                    playerMobile.Kill();
                    FaintPersistence.SetFaint(playerMobile, new Faint(-1, false), true);
                    //TODO Adicionar ação de mover fantasma para algum local, deixa paralizado e sem conseguir falar
                }
            }
            else
            {
                AgePersistence.IncreaseAges();
            }
        }
    }
}

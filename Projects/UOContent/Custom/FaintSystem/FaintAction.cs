using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintUtils
{
    public static void Initialize()
    {
        EventSink.PlayerDeath += OnDeath;
        EventSink.Disconnected += OnDisconnect;
        EventSink.Login += OnLogin;
    }

    private static void OnLogin(Mobile obj)
    {
        throw new System.NotImplementedException();
    }

    private static void OnDisconnect(Mobile obj)
    {
        throw new System.NotImplementedException();
    }

    private static void OnDeath(Mobile obj)
    {
        PlayerMobile playerMobile = obj as PlayerMobile;
        if (playerMobile != null && playerMobile.AccessLevel == AccessLevel.Player && FaintPersistence.GetPlayerFaint(playerMobile) > 0) //Se morrer com 0 pontos, é a morte definitiva
        {
            playerMobile.Frozen = true;
            int oldLightLevel = playerMobile.LightLevel;
            playerMobile.LightLevel = LightCycle.DungeonLevel;

            FaintPersistence.DecreaseFaint(playerMobile);


            if (!(bool)FaintPersistence.GetRunningTimer(playerMobile))
            {
                FaintRecoverTimer faintRecoverTimer = new FaintRecoverTimer(playerMobile);
                FaintPersistence.SetRecoverFaintRunning(playerMobile, true);
                faintRecoverTimer.Start();
            }

            FaintTimer faintTimer = new FaintTimer(playerMobile);
            faintTimer.Start();

            var playerMobileLocation = playerMobile.Location;     //Salva a localização dele, antes de mover
            playerMobile.MoveToWorld(new Point3D(), Map.Felucca); //Move ele para algum outra região
            playerMobile.MoveToWorld(playerMobileLocation, Map.Felucca);
            //TODO Fazer o tratamento para reviver e mandar a pessoa para o local certo
            playerMobile.Resurrect();
            playerMobile.Frozen = false;
            playerMobile.LightLevel = oldLightLevel;
        }
    }
}

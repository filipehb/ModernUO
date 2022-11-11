using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintRecoverTimer : Timer
{
    private readonly PlayerMobile _playerMobile;
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FaintRecoverTimer));

    public FaintRecoverTimer(PlayerMobile playerMobile) : base(TimeSpan.FromMinutes(15.0), TimeSpan.FromSeconds(15.0))
    {
        _playerMobile = playerMobile;
    }

    protected override void OnTick()
    {
        if (_playerMobile.Corpse.Deleted || _playerMobile.Corpse is Corpse { Carved: true })
        {
            _playerMobile.SendMessage("Você sente que sua ligação com seu corpo foi desfeita.");
            return;
        }

        if (FaintPersistence.GetPlayerFaint(_playerMobile) >= 4 || FaintPersistence.GetPlayerFaint(_playerMobile) == -1 ||
            !FaintTimerPersistence.GetPlayerFaintRunning(_playerMobile))
        {
            logger.Debug(
                $"Timer de Faint foi parado para o player {_playerMobile.Account.Username} com o personagem {_playerMobile.Name}"
            );
            FaintTimerPersistence.SetFaintRunning(_playerMobile, false, true);
            Stop();
        }

        FaintPersistence.IncreaseFaint(_playerMobile);
    }
}

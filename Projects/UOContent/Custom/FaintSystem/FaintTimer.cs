using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintTimer : Timer
{
    private readonly PlayerMobile _playerMobile;
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FaintTimer));

    public FaintTimer(PlayerMobile playerMobile) : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromSeconds(5.0))
    {
        _playerMobile = playerMobile;
    }

    protected override void OnTick()
    {
        if (_playerMobile.Alive || _playerMobile.Corpse.Deleted || _playerMobile.Corpse is Corpse && (_playerMobile.Corpse as Corpse)!.Carved)
        {
            return;
        }

        foreach ( Item body in World.Items.Values )
        {
            if ( body is Corpse corpse )
            {
                if ( corpse.Owner == _playerMobile )
                {
                    _playerMobile.MoveToWorld(corpse.Location, Map.Felucca);
                    _playerMobile.Resurrect();

                    // TODO testar se está funcionando ao reviver deletar o deathrobe
                    // TODO Talvez tenha que deletar o corpo tb
                    foreach (Item item in _playerMobile.Items)
                    {
                        if (item is DeathRobe)
                        {
                            item.Delete();
                            break;
                        }
                    }
                }
            }
        }
    }
}

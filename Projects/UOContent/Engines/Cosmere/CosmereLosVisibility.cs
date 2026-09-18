using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public static class CosmereLosVisibility
{
    public static bool RequiresLos(Mobile observer) =>
        CosmereConfig.LosVisibilityEnabled
        && observer is PlayerMobile
        && observer.AccessLevel == AccessLevel.Player;

    public static bool CanSeeMobile(Mobile observer, Mobile target)
    {
        if (!RequiresLos(observer) || target == observer)
        {
            return true;
        }

        return observer.InLOS(target);
    }

    public static bool CanSeeItem(Mobile observer, Item item)
    {
        if (!RequiresLos(observer) || item.Parent != null)
        {
            return true;
        }

        return observer.InLOS(item);
    }

    /// <summary>
    /// Re-evaluate nearby entities for every player client around <paramref name="center"/>
    /// (e.g. after a door opens or closes).
    /// </summary>
    public static void RefreshAround(IPoint3D center, Map map, int range = -1)
    {
        if (!CosmereConfig.LosVisibilityEnabled || map == null || map == Map.Internal)
        {
            return;
        }

        if (range < 0)
        {
            range = Core.GlobalMaxUpdateRange;
        }

        foreach (var state in map.GetClientsInRange(new Point3D(center), range))
        {
            if (state.Mobile is PlayerMobile pm)
            {
                RefreshObserver(pm);
            }
        }
    }

    public static void RefreshObserver(PlayerMobile observer)
    {
        if (observer?.Deleted != false || observer.Map == null || observer.NetState == null)
        {
            return;
        }

        if (!RequiresLos(observer))
        {
            return;
        }

        var ns = observer.NetState;
        var map = observer.Map;
        var loc = observer.Location;
        var range = Core.GlobalMaxUpdateRange;

        var removeEntity = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength].InitializePacket();

        foreach (var m in map.GetMobilesInRange(loc, range))
        {
            if (m == observer || !Utility.InUpdateRange(loc, m.Location))
            {
                continue;
            }

            if (observer.CanSee(m))
            {
                ns.SendMobileIncoming(observer, m);

                if (ns.StygianAbyss)
                {
                    ns.SendMobileHealthbar(m, Healthbar.Yellow);
                    ns.SendMobileHealthbar(m, Healthbar.Poison);
                }

                if (m.IsDeadBondedPet)
                {
                    ns.SendBondedStatus(m.Serial, true);
                }

                m.SendOPLPacketTo(ns);
            }
            else
            {
                OutgoingEntityPackets.CreateRemoveEntity(removeEntity, m.Serial);
                ns.Send(removeEntity);
            }
        }

        foreach (var item in map.GetItemsInRange(loc, range))
        {
            if (item.Parent != null || item.Deleted)
            {
                continue;
            }

            if (!observer.InRange(item.GetWorldLocation(), item.GetUpdateRange(observer)))
            {
                continue;
            }

            if (observer.CanSee(item))
            {
                item.SendInfoTo(ns);
            }
            else
            {
                OutgoingEntityPackets.CreateRemoveEntity(removeEntity, item.Serial);
                ns.Send(removeEntity);
            }
        }
    }
}

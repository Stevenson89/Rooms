using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace arsiy.Rooms.Things
{
    public class CompProperties_ActivateWornPortal : CompProperties
    {
        public CompProperties_ActivateWornPortal()
        {
            compClass = typeof(CompActivateWornPortal);
        }
    }

    
    
    
    
    
    public class CompActivateWornPortal : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            var apparel = parent as Apparel;
            var wearer = apparel?.Wearer;
            if (wearer == null) yield break;

            yield return new Command_Action
            {
                defaultLabel = "YellowRooms_ActivatePortal".Translate(),
                defaultDesc = "YellowRooms_ActivatePortalDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Item/YellowRooms/YellowRooms_PortablePortal"),
                action = () =>
                {
                    if (PortablePortalHelper.TryTeleport(wearer))
                        parent.Destroy();
                }
            };
        }
    }
}

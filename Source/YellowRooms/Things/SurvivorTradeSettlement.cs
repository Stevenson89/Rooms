using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace arsiy.Rooms.Things
{
    
    
    
    
    
    
    
    
    public class SurvivorTradeSettlement : Settlement
    {
        public const int DespawnDays = 30;

        public const int DespawnTicks = DespawnDays * 60000;

        public override Texture2D ExpandingIcon => def.ExpandingIconTexture ?? base.ExpandingIcon;

        public override Material Material => def.Material;

        
        public void StartDespawnCountdown()
        {
            var timeout = GetComponent<TimeoutComp>();
            if (timeout == null)
            {
                RoomsLog.Warning("[Rooms] SurvivorTradeSettlement: TimeoutComp not found on def, the settlement will not despawn.");
                return;
            }
            timeout.StartTimeout(DespawnTicks);
        }

        public int TicksLeftUntilDespawn()
        {
            return GetComponent<TimeoutComp>()?.TicksLeft ?? 0;
        }
    }
}
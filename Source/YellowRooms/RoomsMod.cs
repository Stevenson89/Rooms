using UnityEngine;
using Verse;

namespace arsiy.Rooms
{
    public class RoomsMod : Mod
    {
        public RoomsMod(ModContentPack content) : base(content)
        {
            GetSettings<RoomsSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RoomsSettings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Yellow Rooms";
    }
}

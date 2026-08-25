using Verse;

namespace arsiy.Rooms
{
    public static class RoomsLog
    {
        public static void Message(string text)
        {
            if (RoomsSettings.DebugLogs)
                Log.Message(text);
        }

        public static void Warning(string text)
        {
            if (RoomsSettings.DebugLogs)
                Log.Warning(text);
        }

        public static void Error(string text)
        {
            if (RoomsSettings.DebugLogs)
                Log.Error(text);
        }
    }
}
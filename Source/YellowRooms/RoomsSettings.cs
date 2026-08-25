using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace arsiy.Rooms
{
    public class RoomsSettings : ModSettings
    {
        public int mapSize = 200;
        public int campMapSize = 75;
        public bool randomPawnStranded = false;
        public bool debugLogs = false;

        public List<string> allowedIncidents = new List<string>();
        public List<string> allowedQuests = new List<string>();
        public List<string> allowedGameConditions = new List<string>();

        private HashSet<string> incidentCache = new HashSet<string>();
        private HashSet<string> questCache = new HashSet<string>();
        private HashSet<string> conditionCache = new HashSet<string>();

        private static readonly int[] AllowedSizes = { 100, 150, 200, 250 };
        private static readonly int[] AllowedCampSizes = { 50, 75, 100, 150 };

        private static readonly string[] TabNames = { "Incidents", "Quests", "Game conditions" };

        private static RoomsSettings _instance;

        private static int uiTab;
        private static string uiSearch = string.Empty;
        private static Vector2 uiScroll;
        private static readonly List<string> uiShownNames = new List<string>();
        private static readonly List<string> uiShownLabels = new List<string>();
        private static string uiCacheKey = string.Empty;
        private static int uiTabTotal;

        private const float LineHeight = 24f;

        public static RoomsSettings Instance => _instance;

        public static int MapSize => _instance?.mapSize ?? 200;
        public static int CampMapSize => _instance?.campMapSize ?? 75;
        public static bool RandomPawnStranded => _instance?.randomPawnStranded ?? false;
        public static bool DebugLogs => _instance?.debugLogs ?? false;

        public RoomsSettings()
        {
            _instance = this;
        }

        public bool IsIncidentAllowed(string defName) => incidentCache.Contains(defName);

        public bool IsQuestAllowed(string defName) => questCache.Contains(defName);

        public bool IsGameConditionAllowed(string defName) => conditionCache.Contains(defName);

        public void EnsureDefaultsIfNeeded()
        {
            if (allowedIncidents.Count == 0 && allowedQuests.Count == 0 && allowedGameConditions.Count == 0)
            {
                allowedIncidents = YellowRoomsWhitelistApplier.DefaultIncidents();
                allowedQuests = YellowRoomsWhitelistApplier.DefaultQuests();
                allowedGameConditions = YellowRoomsWhitelistApplier.DefaultGameConditions();
                RebuildCaches();
                Write();
            }
        }

        public void ResetToDefaults()
        {
            allowedIncidents = YellowRoomsWhitelistApplier.DefaultIncidents();
            allowedQuests = YellowRoomsWhitelistApplier.DefaultQuests();
            allowedGameConditions = YellowRoomsWhitelistApplier.DefaultGameConditions();
            ApplyChanges();
        }

        public void ApplyChanges()
        {
            RebuildCaches();
            YellowRoomsWhitelistApplier.Apply();
            Write();
        }

        public void RebuildCaches()
        {
            incidentCache = new HashSet<string>(allowedIncidents);
            questCache = new HashSet<string>(allowedQuests);
            conditionCache = new HashSet<string>(allowedGameConditions);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref mapSize, "mapSize", 200);
            Scribe_Values.Look(ref campMapSize, "campMapSize", 75);
            Scribe_Values.Look(ref randomPawnStranded, "randomPawnStranded", false);
            Scribe_Values.Look(ref debugLogs, "debugLogs", false);
            Scribe_Collections.Look(ref allowedIncidents, "allowedIncidents", LookMode.Value);
            Scribe_Collections.Look(ref allowedQuests, "allowedQuests", LookMode.Value);
            Scribe_Collections.Look(ref allowedGameConditions, "allowedGameConditions", LookMode.Value);
            allowedIncidents ??= new List<string>();
            allowedQuests ??= new List<string>();
            allowedGameConditions ??= new List<string>();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildCaches();
            }
        }

        public static void DoSettingsWindowContents(Rect inRect)
        {
            var settings = _instance;
            if (settings == null) return;

            settings.EnsureDefaultsIfNeeded();

            float y = 4f;
            float x = 0f;
            float width = inRect.width;

            Widgets.Label(new Rect(x, y, width, LineHeight), "Yellow Rooms Map Size: " + settings.mapSize + "x" + settings.mapSize);
            y += LineHeight;
            for (int i = 0; i < AllowedSizes.Length; i++)
            {
                var rect = new Rect(x + 20f + i * ((width - 20f) / AllowedSizes.Length), y, (width - 20f) / AllowedSizes.Length, LineHeight);
                if (Widgets.RadioButtonLabeled(rect, AllowedSizes[i] + "x" + AllowedSizes[i], settings.mapSize == AllowedSizes[i]))
                {
                    settings.mapSize = AllowedSizes[i];
                    settings.Write();
                }
            }
            y += LineHeight + 8f;

            Widgets.Label(new Rect(x, y, width, LineHeight), "Yellow Rooms Camp Map Size: " + settings.campMapSize + "x" + settings.campMapSize);
            y += LineHeight;
            for (int i = 0; i < AllowedCampSizes.Length; i++)
            {
                var rect = new Rect(x + 20f + i * ((width - 20f) / AllowedCampSizes.Length), y, (width - 20f) / AllowedCampSizes.Length, LineHeight);
                if (Widgets.RadioButtonLabeled(rect, AllowedCampSizes[i] + "x" + AllowedCampSizes[i], settings.campMapSize == AllowedCampSizes[i]))
                {
                    settings.campMapSize = AllowedCampSizes[i];
                    settings.Write();
                }
            }
            y += LineHeight + 8f;

            y = DrawWhitelistSection(settings, x, y, width);

            bool rps = settings.randomPawnStranded;
            Widgets.CheckboxLabeled(new Rect(x, y, width, LineHeight), "Random pawn stranded in the rooms (off by default)", ref rps);
            if (rps != settings.randomPawnStranded)
            {
                settings.randomPawnStranded = rps;
                settings.Write();
            }
            y += LineHeight;

            bool dl = settings.debugLogs;
            Widgets.CheckboxLabeled(new Rect(x, y, width, LineHeight), "Debug logs (in Russian: Выводить логи отладки)", ref dl);
            if (dl != settings.debugLogs)
            {
                settings.debugLogs = dl;
                settings.Write();
            }
            y += LineHeight;
        }

        private static float DrawWhitelistSection(RoomsSettings settings, float x, float y, float width)
        {
            Widgets.Label(new Rect(x, y, width, LineHeight), "Yellow Rooms layer events (whitelist only)");
            y += LineHeight + 4f;

            float tabWidth = (width - 8f) / 3f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                if (Widgets.ButtonText(new Rect(x + i * (tabWidth + 4f), y, tabWidth, 26f), TabNames[i], uiTab == i))
                {
                    uiTab = i;
                    uiScroll = Vector2.zero;
                }
            }
            y += 30f;

            float btnWidth = 92f;
            string search = Widgets.TextField(new Rect(x, y, width - btnWidth * 3f - 16f, 24f), uiSearch);
            if (search != uiSearch)
            {
                uiSearch = search;
                uiScroll = Vector2.zero;
            }
            if (Widgets.ButtonText(new Rect(x + width - btnWidth * 3f - 8f, y, btnWidth, 24f), "All on"))
                SetAllInTab(settings, true);
            if (Widgets.ButtonText(new Rect(x + width - btnWidth * 2f - 4f, y, btnWidth, 24f), "All off"))
                SetAllInTab(settings, false);
            if (Widgets.ButtonText(new Rect(x + width - btnWidth, y, btnWidth, 24f), "Reset tab"))
                ResetTab(settings);
            y += 28f;

            RebuildShownList();

            float listHeight = 190f;
            float itemHeight = 22f;
            var current = CurrentAllowedList(settings);

            if (uiShownNames.Count > 0)
            {
                float viewHeight = Math.Max(uiShownNames.Count * itemHeight, listHeight);
                Widgets.BeginScrollView(new Rect(x, y, width, listHeight), ref uiScroll, new Rect(0f, 0f, width - 24f, viewHeight));
                for (int i = 0; i < uiShownNames.Count; i++)
                {
                    var row = new Rect(4f, i * itemHeight, width - 32f, itemHeight);
                    bool on = current.Contains(uiShownNames[i]);
                    bool oldOn = on;
                    Widgets.CheckboxLabeled(row, uiShownLabels[i], ref on);
                    if (on != oldOn)
                    {
                        if (on)
                        {
                            if (!current.Contains(uiShownNames[i])) current.Add(uiShownNames[i]);
                        }
                        else
                        {
                            current.Remove(uiShownNames[i]);
                        }
                        settings.ApplyChanges();
                    }
                }
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(new Rect(x, y, width, listHeight), "No events match the search.");
            }
            y += listHeight + 4f;

            Widgets.Label(new Rect(x, y, width, LineHeight),
                TabNames[uiTab] + ": " + current.Count + " allowed, " + uiShownNames.Count + " shown of " + uiTabTotal);
            y += LineHeight;

            if (Widgets.ButtonText(new Rect(x, y, width, 26f), "Reset all to defaults"))
            {
                settings.ResetToDefaults();
                uiScroll = Vector2.zero;
            }
            y += 30f;

            return y;
        }

        private static void SetAllInTab(RoomsSettings settings, bool on)
        {
            var current = CurrentAllowedList(settings);
            if (on)
            {
                foreach (var def in TabDefs())
                    if (!current.Contains(def.defName)) current.Add(def.defName);
            }
            else
            {
                current.Clear();
            }
            settings.ApplyChanges();
        }

        private static void ResetTab(RoomsSettings settings)
        {
            var current = CurrentAllowedList(settings);
            current.Clear();
            current.AddRange(DefaultListForTab());
            settings.ApplyChanges();
        }

        private static List<string> DefaultListForTab()
        {
            return uiTab switch
            {
                1 => YellowRoomsWhitelistApplier.DefaultQuests(),
                2 => YellowRoomsWhitelistApplier.DefaultGameConditions(),
                _ => YellowRoomsWhitelistApplier.DefaultIncidents(),
            };
        }

        private static List<string> CurrentAllowedList(RoomsSettings settings)
        {
            return uiTab switch
            {
                1 => settings.allowedQuests,
                2 => settings.allowedGameConditions,
                _ => settings.allowedIncidents,
            };
        }

        private static List<Def> TabDefs()
        {
            switch (uiTab)
            {
                case 1: return DefDatabase<QuestScriptDef>.AllDefsListForReading.Cast<Def>().ToList();
                case 2: return DefDatabase<GameConditionDef>.AllDefsListForReading.Cast<Def>().ToList();
                default: return DefDatabase<IncidentDef>.AllDefsListForReading.Cast<Def>().ToList();
            }
        }

        private static void RebuildShownList()
        {
            string key = uiTab + "|" + uiSearch;
            if (uiCacheKey == key) return;
            uiCacheKey = key;

            uiShownNames.Clear();
            uiShownLabels.Clear();

            string search = uiSearch.Trim().ToLowerInvariant();
            var defs = TabDefs();
            uiTabTotal = defs.Count;

            foreach (var def in defs)
            {
                string display = def.label.NullOrEmpty() ? def.defName : def.label.Translate();
                if (search.Length > 0 &&
                    def.defName.ToLowerInvariant().IndexOf(search, StringComparison.Ordinal) < 0 &&
                    display.ToLowerInvariant().IndexOf(search, StringComparison.Ordinal) < 0)
                {
                    continue;
                }
                uiShownNames.Add(def.defName);
                uiShownLabels.Add(display);
            }

            var combined = new List<(string name, string label)>();
            for (int i = 0; i < uiShownNames.Count; i++)
                combined.Add((uiShownNames[i], uiShownLabels[i]));
            combined.Sort((a, b) => string.CompareOrdinal(a.label, b.label));

            uiShownNames.Clear();
            uiShownLabels.Clear();
            foreach (var pair in combined)
            {
                uiShownNames.Add(pair.name);
                uiShownLabels.Add(pair.label);
            }
        }
    }
}
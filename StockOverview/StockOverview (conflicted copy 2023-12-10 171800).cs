using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using TerminalApi;
using static TerminalApi.Events.Events;
using static TerminalApi.TerminalApi;

namespace StockOverview
{
    public static class PluginInfo
    {
        public const string PLUGIN_NAME = "StockOverview";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string PLUGIN_GUID = "squall4120.stockoverview";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("atomic.terminalapi")]

    public class StockOverview : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Adds stock command, 'view' is the verb. Verbs are optional
            AddCommand("stock", "View a detailed breakdown of collected scrap.\n", "view", true);
        }
    }
}

using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using static TerminalApi.TerminalApi;

namespace StockOverview
{
    public static class PluginInfo
    {
        public const string PLUGIN_NAME = "StockOverview";
        public const string PLUGIN_VERSION = "1.0.3";
        public const string PLUGIN_GUID = "squall4120.stockoverview";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("atomic.terminalapi")]

    public class StockOverview : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            // Create "stock" keyword and node to trigger "stockoverview" terminalEvent
            TerminalNode stockNode = CreateTerminalNode("Scanning for scrap on ship...\n", true, "stockoverview");
            TerminalKeyword stockKeyword = CreateTerminalKeyword("stock", true, stockNode);
            AddTerminalKeyword(stockKeyword);
        }

        public static string BuildOverview()
        {
            // Display title
            string displayText = "STOCK OVERVIEW" + "\n\n";

            // Get all objects that can be picked up from inside the ship. Also remove items which technically have
            // scrap value but don't actually add to your quota.
            var loot = GameObject.Find("/Environment/HangarShip").GetComponentsInChildren<GrabbableObject>()
                .Where(obj => obj.name != "ClipboardManual" && obj.name != "StickyNoteItem");

            string totalQuantity = "Quantity of all items: {0}";
            string totalValue = "Total value of all items: ${0:F0}";

            // Display the quantity and total value of all items first
            displayText += string.Format(totalQuantity, loot.Count()) + "\n";
            displayText += string.Format(totalValue, loot.Sum(item => item.scrapValue)) + "\n";
            displayText += "____________________________" + "\n\n";

            // Group items by item name and sort alphabetically
            var groupedLoot = loot.GroupBy(item => item.itemProperties.itemName)
                .OrderBy(group => group.Key).ToList();

            string groupHeader = "{0} / x{1} / Total Value: ${2:F0}";
            string singleItem = "  {0} - Value: ${1:F0}";

            foreach (var group in groupedLoot)
            {
                // Display item name / quantity of item in group / total value of group
                displayText += string.Format(groupHeader, group.Key, group.Count(), group.Sum(item => item.scrapValue)) + "\n";

                // Display each individual item with name and value
                group.Do(item => displayText += string.Format(singleItem, item.itemProperties.itemName, item.scrapValue) + "\n");

                // Add a linebreak between groups
                displayText += "\n";
            }

            return displayText;
        }
    }

    [HarmonyPatch(typeof(Terminal))]
    [HarmonyPatch("RunTerminalEvents")]

    public class RunTerminalEvents_Patch : MonoBehaviour
    {
        static IEnumerator PostfixCoroutine(Terminal __instance, TerminalNode node)
        {
            if (string.IsNullOrWhiteSpace(node.terminalEvent) || node.terminalEvent != "stockoverview")
            {
                yield break;
            }

            // Call output constructor
            node.displayText = StockOverview.BuildOverview();
        }

        static void Postfix(Terminal __instance, TerminalNode node)
        {
            // Start the coroutine
            __instance.StartCoroutine(PostfixCoroutine(__instance, node));
        }
    }
}
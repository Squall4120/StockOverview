using System.Collections;
using System.Globalization;
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
        public const string PLUGIN_VERSION = "1.0.0";
        public const string PLUGIN_GUID = "squall4120.stockoverview";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("atomic.terminalapi", MinimumDependencyVersion: "1.3.2")]

    public class StockOverview : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            // Patch Harmony with mod ID
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            // Create "stock" keyword and node to trigger "stockoverview" terminalEvent
            TerminalNode terminalNode = CreateTerminalNode("Scanning for scrap on ship...\n", true, "stockoverview");
            TerminalKeyword stockKeyword = CreateTerminalKeyword("stock", true, terminalNode);
            AddTerminalKeyword(stockKeyword);
        }

        public static string BuildOverview()
        {
            // Display title
            string displayText = "STOCK OVERVIEW\n\n";

            // Get all objects that can be picked up from inside the ship. Also remove items which technically have
            // scrap value but don't actually add to your quota.
            var loot = GameObject.Find("/Environment/HangarShip").GetComponentsInChildren<GrabbableObject>()
                .Where(obj => obj.name != "ClipboardManual" && obj.name != "StickyNoteItem");

            // Display the quantity and total value of all scrap first
            displayText += string.Format("Quantity of all scrap: {0}", loot.Count()) + "\n";
            displayText += string.Format("Total value of all scrap: ${0:F0}", loot.Sum(scrap => scrap.scrapValue)) + "\n";
            displayText += "____________________________";
            displayText += "\n\n";

            // Group scrap by item name and sort alphabetically
            var groupedLoot = loot.GroupBy(scrap => scrap.itemProperties.itemName)
                .OrderBy(group => group.Key).ToList();

            // Display each group with detailed properties
            foreach (var group in groupedLoot)
            {
                // Display item name / quantity of item in group / total value of group
                displayText += string.Format("{0} / x{1} / Total Value: ${2:F0}\n", group.Key, group.Count(), group.Sum(item => item.scrapValue));

                // Display each individual item with name and value
                group.Do(item => displayText += string.Format("  {0} - Value: ${1:F0}\n", item.itemProperties.itemName, item.scrapValue));

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
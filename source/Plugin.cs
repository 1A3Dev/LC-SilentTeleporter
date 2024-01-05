using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace SilentTeleporter
{
    [BepInPlugin(modGUID, "SilentTeleporter", modVersion)]
    internal class PluginLoader : BaseUnityPlugin
    {
        internal const string modGUID = "Dev1A3.SilentTeleporter";

        private readonly Harmony harmony = new Harmony(modGUID);

        private const string modVersion = "1.0.0";

        private static bool initialized;

        internal static ManualLogSource logSource;

        public static PluginLoader Instance { get; private set; }

        private void Awake()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Instance = this;

            logSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            Assembly patches = Assembly.GetExecutingAssembly();
            harmony.PatchAll(patches);

            logSource.LogInfo("Loaded SilentTeleporter");
        }
    }

    [HarmonyPatch]
    internal static class TeleportPatch
    {
        [HarmonyPatch(typeof(ShipTeleporter), "beamUpPlayer", MethodType.Enumerator)]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool hasSkipped = false;
            bool startFound = false;
            bool shouldSkip = false;
            foreach (var instruction in instructions)
            {
                if (!hasSkipped && instruction.opcode == OpCodes.Call && instruction.operand.ToString() == "Void SetPlayerTeleporterId(GameNetcodeStuff.PlayerControllerB, Int32)")
                {
                    //PluginLoader.logSource.LogInfo("Found SetPlayerTeleporterId");
                    startFound = true;
                }
                else if (!hasSkipped && startFound && instruction.opcode == OpCodes.Ldarg_0)
                {
                    //PluginLoader.logSource.LogInfo("Found Ldarg_0");
                    shouldSkip = true;
                    hasSkipped = true;
                }
                else if (instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "Teleport A")
                {
                    //PluginLoader.logSource.LogInfo("Found Teleport A");
                    shouldSkip = false;
                }

                if (!shouldSkip)
                {
                    //PluginLoader.logSource.LogInfo($"[] {instruction.opcode}: {instruction.operand}");
                    newInstructions.Add(instruction);
                }
            }

            return newInstructions.AsEnumerable();
        }
    }
}
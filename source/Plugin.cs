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
            var newInstructions = new List<CodeInstruction>(instructions);

            // I defo overcomplicated this but it works so yeah...

            int startIndex = 99; // IL_015E: ldfld     class GameNetcodeStuff.PlayerControllerB ShipTeleporter/'<beamUpPlayer>d__32'::'<playerToBeamUp>5__2'
            int endIndex = 135; // IL_01DD: callvirt  instance void [UnityEngine.AudioModule]UnityEngine.AudioSource::PlayOneShot(class [UnityEngine.AudioModule]UnityEngine.AudioClip)

            var startInstruction = newInstructions[startIndex];
            if (startInstruction != null && startInstruction.opcode == OpCodes.Ldfld && startInstruction.operand.ToString() == "GameNetcodeStuff.PlayerControllerB <playerToBeamUp>5__2")
            {
                var endInstruction = newInstructions[endIndex];
                if (endInstruction.opcode == OpCodes.Callvirt && endInstruction.operand.ToString() == "Void PlayOneShot(UnityEngine.AudioClip)")
                {
                    newInstructions.RemoveRange(startIndex, (endIndex - startIndex) + 2);
                    PluginLoader.logSource.LogInfo("Replaced Teleport Code");
                    //int index = 0;
                    //foreach (var instruction in newInstructions)
                    //{
                    //    PluginLoader.logSource.LogInfo($"[{index}] {instruction.opcode}: {instruction.operand}");
                    //    index++;
                    //}
                }
            }

            return newInstructions.AsEnumerable();
        }
    }
}
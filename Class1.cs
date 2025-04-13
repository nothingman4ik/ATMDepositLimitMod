using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using ScheduleOne.Money;
using ScheduleOne.UI.ATM;

[assembly: MelonInfo(typeof(ATMDepositLimitMod.ATMDepositLimitMod), "ATM Deposit Limit Mod", "1.1.0", "YourName")]
[assembly: MelonGame("ScheduleOne", "ScheduleOne")]

namespace ATMDepositLimitMod
{
    public class ATMDepositLimitMod : MelonMod
    {
        private const float DEFAULT_LIMIT = 25000f;
        private float _depositLimit = DEFAULT_LIMIT;
        private float _configLimit = DEFAULT_LIMIT;

        public override void OnInitializeMelon()
        {
            LoadConfig();
            try
            {
                HarmonyInstance.PatchAll();
                MelonLogger.Msg($"[ATM Deposit Limit Mod] Successfully patched all methods with deposit limit set to {_configLimit}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ATM Deposit Limit Mod] Failed to patch methods: " + ex.Message);
            }
        }

        private void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(MelonEnvironment.UserDataDirectory, "Mods", "atmconfig.txt");
                string configDir = Path.GetDirectoryName(configPath);

                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                if (!File.Exists(configPath))
                {
                    string defaultConfig = "DepositLimit=25000";
                    File.WriteAllText(configPath, defaultConfig);
                    MelonLogger.Msg("[ATM Deposit Limit Mod] Created default config file");
                }

                string[] lines = File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("DepositLimit="))
                    {
                        if (float.TryParse(line.Split('=')[1], out float limit))
                        {
                            _configLimit = limit;
                            _depositLimit = limit;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ATM Deposit Limit Mod] Error handling config file: " + ex.Message);
            }
        }

        public float GetDepositLimit()
        {
            return _depositLimit;
        }
    }

    [HarmonyPatch(typeof(ATM))]
    [HarmonyPatch(MethodType.Constructor)]
    public static class ATM_Constructor_Patch
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var mod = Melon<ATMDepositLimitMod>.Instance;
            float limit = mod.GetDepositLimit();

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float f && f == 10000f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, limit);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ATMInterface))]
    public static class ATMInterface_Patches
    {
        [HarmonyPatch("get_remainingAllowedDeposit")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler_RemainingAllowedDeposit(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceDepositLimit(instructions);
        }

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler_Update(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceDepositLimit(instructions);
        }

        private static IEnumerable<CodeInstruction> ReplaceDepositLimit(IEnumerable<CodeInstruction> instructions)
        {
            var mod = Melon<ATMDepositLimitMod>.Instance;
            float limit = mod.GetDepositLimit();

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float f && f == 10000f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, limit);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}

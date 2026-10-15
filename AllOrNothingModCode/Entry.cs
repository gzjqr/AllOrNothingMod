using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace AllOrNothingMod;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    public const string ModId = "AllOrNothingMod";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 新增：激活 Harmony 补丁
        var harmony = new Harmony(ModId);
        harmony.PatchAll(assembly);

        Logger.Info("AllOrNothingMod initialized.");
    }
}
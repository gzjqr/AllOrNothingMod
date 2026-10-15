using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Rewards;

namespace AllOrNothingMod.Hooks;

public static class CardRewardRegistry
{
    private static CardReward? _current;

    public static void SetCurrent(CardReward reward) => _current = reward;

    public static CardReward? GetCurrent() => _current;
}

[HarmonyPatch(typeof(CardReward))]
public static class CardRewardOnSelectPatch
{
    [HarmonyPatch("OnSelect")]
    [HarmonyPrefix]
    public static void Prefix(CardReward __instance)
    {
        CardRewardRegistry.SetCurrent(__instance);
    }
}

/// <summary>
/// 拦截 Hook.ShouldAllowSelectingMoreCardRewards，在"全拿了"进行期间强制允许多选。
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook))]
public static class HookShouldAllowSelectingMorePatch
{
    [HarmonyPatch("ShouldAllowSelectingMoreCardRewards")]
    [HarmonyPostfix]
    public static void Postfix(ref bool __result)
    {
        if (CardRewardScreenPatch.TakeAllRequested)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen))]
public static class CardRewardScreenPatch
{
    public static bool TakeAllRequested;

    private static readonly FieldInfo RewardAlternativesContainerField =
        AccessTools.Field(typeof(NCardRewardSelectionScreen), "_rewardAlternativesContainer");

    private static readonly FieldInfo OptionsField =
        AccessTools.Field(typeof(NCardRewardSelectionScreen), "_options");

    private static readonly FieldInfo CompletionSourceField =
        AccessTools.Field(typeof(NCardRewardSelectionScreen), "_completionSource");

    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    public static void Postfix(NCardRewardSelectionScreen __instance)
    {
        var container = RewardAlternativesContainerField?.GetValue(__instance) as Control;
        var options = OptionsField?.GetValue(__instance) as IReadOnlyList<CardCreationResult>;

        Entry.Logger.Info($"[AllOrNothing] Postfix, options.Count = {options?.Count ?? -1}");

        if (container == null || options == null || options.Count <= 1)
            return;

        if (container.GetNodeOrNull<Button>("TakeAllButton") != null)
            return;

        var button = new Button
        {
            Name = "TakeAllButton",
            Text = "全拿了！",
            CustomMinimumSize = new Vector2(200f, 50f)
        };

        button.Pressed += () =>
        {
            TaskHelper.RunSafely(TakeAllAsync(__instance));
        };

        container.AddChild(button);
    }

    private static async Task TakeAllAsync(NCardRewardSelectionScreen screen)
    {
        TakeAllRequested = true;

        var container = RewardAlternativesContainerField?.GetValue(screen) as Control;
        var button = container?.GetNodeOrNull<Button>("TakeAllButton");
        if (button != null) button.Disabled = true;

        var options = OptionsField?.GetValue(screen) as IReadOnlyList<CardCreationResult>;
        Entry.Logger.Info($"[AllOrNothing] TakeAllAsync started, options.Count = {options?.Count ?? -1}");
        if (options == null || options.Count == 0)
        {
            TakeAllRequested = false;
            return;
        }

        int totalToTake = options.Count;
        int taken = 0;

        for (int i = 0; i < totalToTake; i++)
        {
            TaskCompletionSource<int?>? source = null;
            for (int wait = 0; wait < 30; wait++)
            {
                var candidate = CompletionSourceField?.GetValue(screen) as TaskCompletionSource<int?>;
                if (candidate != null && !candidate.Task.IsCompleted)
                {
                    source = candidate;
                    break;
                }
                await Task.Delay(100);
            }

            if (source == null)
            {
                Entry.Logger.Info($"[AllOrNothing] No completion source at iteration {i}, stopping");
                break;
            }

            try
            {
                source.SetResult(0);
                taken++;
                Entry.Logger.Info($"[AllOrNothing] SetResult(0), taken={taken}/{totalToTake}");
            }
            catch (System.Exception ex)
            {
                Entry.Logger.Info($"[AllOrNothing] SetResult failed: {ex.Message}");
                break;
            }

            await Task.Delay(400);
        }

        // 最后一次“取消选择”，让原版 while 循环收尾
        for (int wait = 0; wait < 30; wait++)
        {
            var finalSource = CompletionSourceField?.GetValue(screen) as TaskCompletionSource<int?>;
            if (finalSource != null && !finalSource.Task.IsCompleted)
            {
                try { finalSource.SetResult(null); } catch { }
                Entry.Logger.Info("[AllOrNothing] SetResult(null) to end loop");
                break;
            }
            await Task.Delay(100);
        }

        TakeAllRequested = false;
        Entry.Logger.Info($"[AllOrNothing] TakeAllAsync finished, taken={taken}");
    }
}
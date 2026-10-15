using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace AllOrNothingMod.Characters;

public sealed class AllOrNothingModPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "AllOrNothingMod";
    public override Color LabOutlineColor => AllOrNothingModCharacter.ThemeColor;

    // 即使模板暂时没有示例药水，也先把角色药水池结构留好。
    // AssetProfile 里的资源路径不存在时，RitsuLib 会输出诊断并回退；模板这里提供真实 PNG 占位。
    public override string? BigEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_big.png";
    public override string? TextEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_text.png";
}

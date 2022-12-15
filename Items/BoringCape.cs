using Terraria.ModLoader;

namespace ClothDemo.Items;

[AutoloadEquip(EquipType.Back)]
public class BoringCape : BaseCape
{
    protected override string CapeDataKey => "Default";
}
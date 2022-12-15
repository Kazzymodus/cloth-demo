using ClothDemo.Effects;
using ClothDemo.Effects.DataStructures;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace ClothDemo.Items;

[AutoloadEquip(EquipType.Back)]
public class EmberCape : ModItem
{
    [CloneByReference] private CapeData _capeData;

    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("'It breezes in the wind'\nCan be dyed");
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.accessory = true;
        Item.vanity = true;
        Item.width = 24;
        Item.height = 24;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (!hideVisual)
        {
            _capeData ??= ModContent.GetInstance<ClothDemo>().GetCapeData("Default");
            player.GetModPlayer<ClothDemoPlayer>().UpdateEmberCape(_capeData);
        }
    }

    public override void UpdateVanity(Player player)
    {
        _capeData ??= ModContent.GetInstance<ClothDemo>().GetCapeData("Default");
        player.GetModPlayer<ClothDemoPlayer>().UpdateEmberCape(_capeData);
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood)
            .Register();
    }
}
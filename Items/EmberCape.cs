﻿using Terraria.ModLoader;

namespace ClothDemo.Items;

[AutoloadEquip(EquipType.Back)]
public class EmberCape : BaseCape
{
    protected override string CapeDataKey => "Ember";
}
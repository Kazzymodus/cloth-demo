using ClothDemo.Effects;
using ClothDemo.Effects.DataStructures;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace ClothDemo;

public class ClothDemoPlayer : ModPlayer
{
    private bool _hasEmberCapeEquipped;
    private bool _hasEmberCapeEquippedLastFrame;
    private CapeModel _emberCape;

    public void UpdateEmberCape(CapeData capeData)
    {
        if (Main.dedServ || _hasEmberCapeEquipped)
            return;

        _hasEmberCapeEquipped = true;

        var capeOffset = new Vector2(capeData.CapeXOffset * Player.direction, capeData.CapeYOffset);
        var capePosition = Player.Center + capeOffset;

        if (_emberCape == null || !_hasEmberCapeEquippedLastFrame)
        {
            _emberCape?.Dispose();
            _emberCape = new CapeModel(capePosition, Player.direction, capeData.Dimensions, capeData.Anchor,
                capeData.PhysicalProperties, capeData.Shader.PassName);
        }

        _emberCape.Update(capePosition, Player.direction, capeData.DefaultDamping, capeData.ConstraintPasses);
    }


    public override void ResetEffects()
    {
        if (_hasEmberCapeEquippedLastFrame && !_hasEmberCapeEquipped)
        {
            _emberCape.Dispose();
            _emberCape = null;
        }

        _hasEmberCapeEquippedLastFrame = _hasEmberCapeEquipped;
        _hasEmberCapeEquipped = false;
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a,
        ref bool fullBright)
    {
        if (!_hasEmberCapeEquipped || _emberCape == null || drawInfo.shadow != 0f)
            return;

        var shader = GameShaders.Armor.GetSecondaryShader(Player.cBack, Player);

        _emberCape.Draw(Player, shader);
    }
}
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
    private bool _hasCapeEquipped;
    private bool _hasCapeEquippedLastFrame;
    private int? _lastCapeDataId;
    private CapeModel _cape;

    public void UpdateCape(CapeData capeData)
    {
        if (Main.dedServ || _hasCapeEquipped)
            return;

        _hasCapeEquipped = true;

        var capeOffset = new Vector2(capeData.CapeXOffset * Player.direction, capeData.CapeYOffset);
        var capePosition = Player.Center + capeOffset;

        if (_cape == null || !_hasCapeEquippedLastFrame || (_lastCapeDataId.HasValue && _lastCapeDataId != capeData.Id))
        {
            _cape?.Dispose();
            _cape = new CapeModel(capePosition, Player.direction, capeData.Dimensions, capeData.Anchor,
                capeData.PhysicalProperties, capeData.Shader.PassName);
        }

        _cape.Update(capePosition, Player.direction, capeData.DefaultDamping, capeData.ConstraintPasses);

        _lastCapeDataId = capeData.Id;
    }


    public override void ResetEffects()
    {
        if (_hasCapeEquippedLastFrame && !_hasCapeEquipped)
        {
            _cape.Dispose();
            _cape = null;
        }

        _hasCapeEquippedLastFrame = _hasCapeEquipped;
        _hasCapeEquipped = false;
    }

    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a,
        ref bool fullBright)
    {
        if (!_hasCapeEquipped || _cape == null || drawInfo.shadow != 0f)
            return;

        var shader = GameShaders.Armor.GetSecondaryShader(Player.cBack, Player);

        _cape.Draw(Player, shader);
    }
}
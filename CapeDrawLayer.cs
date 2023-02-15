using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ClothDemo;

public class CapeDrawLayer : PlayerDrawLayer
{
    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
        drawInfo.drawPlayer.GetModPlayer<ClothDemoPlayer>().ShouldDrawCape;

    // This is irrelevant for the cape model itself, but not for any supplementary textures.
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.shadow != 0f)
            return;

        var modPlayer = drawInfo.drawPlayer.GetModPlayer<ClothDemoPlayer>();
        if (!modPlayer.ShouldDrawCape)
            return;

        // This ignores layers entirely and gets drawn before anything else, more out of necessity than out of choice.
        modPlayer.DrawCape();
    }
}
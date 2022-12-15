using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClothDemo.Effects;
using ClothDemo.Effects.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace ClothDemo;

public class ClothDemo : Mod
{
    private IDictionary<string, CapeData> _capeDataCollection;

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        var capeDataFileBytes = GetFileBytes("CapeData.json");
        var capeDataJson = Encoding.UTF8.GetString(capeDataFileBytes);
        _capeDataCollection = JsonConvert.DeserializeObject<IDictionary<string, CapeData>>(capeDataJson);

        var capeDataId = 0;
        foreach (var capeData in _capeDataCollection)
            capeData.Value.Id = capeDataId++;

        var shaderRef = new Ref<Effect>(ModContent
            .Request<Effect>("ClothDemo/Effects/Shader", AssetRequestMode.ImmediateLoad).Value);

        foreach (var (_, capeData) in _capeDataCollection)
        {
            var shaderData = new MiscShaderData(shaderRef, capeData.Shader.PassName);

            UseValueIfNotNull(capeData.Shader.Image, shaderData.UseImage1);
            UseValueIfNotNull(capeData.Shader.Color, shaderData.UseColor);
            UseValueIfNotNull(capeData.Shader.SecondaryColor, shaderData.UseSecondaryColor);
            UseValueIfNotNull(capeData.Shader.Opacity, shaderData.UseOpacity);

            GameShaders.Misc[capeData.Shader.PassName] = shaderData;
        }
    }

    // If these were local functions they couldn't have the same name, so I decided to just leave them at
    // the class scope.
    private static void UseValueIfNotNull<T>(T value, Func<T, MiscShaderData> method)
        where T : class
    {
        if (value != null)
            method(value);
    }

    private static void UseValueIfNotNull<T>(T? value, Func<T, MiscShaderData> method)
        where T : struct
    {
        if (value.HasValue)
            method(value.Value);
    }

    public CapeData GetCapeData(string key)
    {
        return _capeDataCollection[key];
    }
}
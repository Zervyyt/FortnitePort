using System;
using CUE4Parse.UE4.Assets.Exports.Material;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.Viewer.Shaders;

public class Material : IDisposable
{
    public UMaterialInterface Interface;
    private Texture? Diffuse;
    private Texture? Normals;
    private Texture? SpecularMasks;
    private Texture? Mask;

    public Material(UMaterialInterface materialInterface)
    {
        Interface = materialInterface;
        var parameters = new CMaterialParams2();
        materialInterface.GetParams(parameters, EMaterialFormat.AllLayers);
        
        if (parameters.TryGetTexture2d(out var diffuse, "Diffuse"))
        {
            Diffuse = new Texture(diffuse);
            Diffuse.Bind();
        }
        
        if (parameters.TryGetTexture2d(out var normals, "Normals"))
        {
            Normals = new Texture(normals);
            Normals.Bind();
        }
        
        if (parameters.TryGetTexture2d(out var specular, "SpecularMasks"))
        {
            SpecularMasks = new Texture(specular);
            SpecularMasks.Bind();
        }
        
        if (parameters.TryGetTexture2d(out var mask, "M"))
        {
            Mask = new Texture(mask);
            Mask.Bind(); 
        }
        
    }

    public void Bind()
    {
        Diffuse?.Bind(TextureUnit.Texture0);
        Normals?.Bind(TextureUnit.Texture1);
        SpecularMasks?.Bind(TextureUnit.Texture2);
        Mask?.Bind(TextureUnit.Texture3);
        AppVM.MainVM.ModelViewer?.Renderer.Skybox.Cubemap.Bind(TextureUnit.Texture4);
    }

    public void Dispose()
    {
        Diffuse?.Dispose();
        Normals?.Dispose();
        SpecularMasks?.Dispose();
        Mask?.Dispose();
    }
}
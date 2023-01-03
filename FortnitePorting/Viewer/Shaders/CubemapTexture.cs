using System;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;

public class CubemapTexture : IDisposable
{
    private readonly int Handle;

    public int Width;
    public int Height;
    
    public CubemapTexture(params string[] textures)
    {
        Handle = GL.GenTexture();
        Bind();

        for (int t = 0; t < textures.Length; t++)
        {
            ProcessPixels(textures[t], TextureTarget.TextureCubeMapPositiveX + t);
        }

        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
    }

    private void ProcessPixels(string texture, TextureTarget target)
    {
        var info = Application.GetResourceStream(new Uri($"/FortnitePorting;component/Resources/Shaders/{texture}.png", UriKind.Relative));
        var img = ImageResult.FromStream(info.Stream, ColorComponents.RedGreenBlueAlpha);
        Width = img.Width;
        Height = img.Height;
        GL.TexImage2D(target, 0, PixelInternalFormat.Rgba8, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, img.Data);
    }

    public void Bind(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }
    
    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        GL.DeleteTexture(Handle);
    }
}
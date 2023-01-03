using System;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using FortnitePorting.Views.Extensions;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;

public class Texture : IDisposable
{
    private readonly int Handle;

    public int Width;
    public int Height;
    public ImageResult Image;
    
    public Texture(UTexture2D texture)
    {
        Image = texture.ToImageResult();
        Handle = GL.GenTexture();
        Bind();
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Image.Width, Image.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, Image.Data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
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
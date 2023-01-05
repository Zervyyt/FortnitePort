using System;
using System.IO;
using System.Windows;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using FortnitePorting.Views.Extensions;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using StbImageSharp;

public class Texture : IDisposable
{
    private readonly int Handle;

    public int Width;
    public int Height;
    public ImageResult Image;
    
    public Texture(UTexture2D texture)
    {
        Handle = GL.GenTexture();
        Bind();
        
        var firstMip = texture.GetFirstMip();
        TextureDecoder.DecodeTexture(firstMip, texture.Format, texture.isNormalMap, ETexturePlatform.DesktopMobile, out var data, out _);
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, texture.SRGB ? PixelInternalFormat.Srgb : PixelInternalFormat.Rgb, firstMip.SizeX, firstMip.SizeY, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

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
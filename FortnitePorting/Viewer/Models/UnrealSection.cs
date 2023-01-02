using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Viewer.Buffers;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SkiaSharp;
using StbImageSharp;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace FortnitePorting.Viewer.Models;

public class UnrealSection : IRenderable
{
    public int Handle { get; set; }
    public Buffer<float> VBO { get; set; }
    public Buffer<uint> EBO { get; set; }
    public VertexArray<float> VAO { get; set; }
    public Shader Shader { get; set; }
    public Matrix4 Transform { get; set; }
    
    private List<float> Vertices = new();
    private List<uint> Indices = new();
    private ImageResult? Texture;

    public UnrealSection(CSkelMeshLod lod, CMeshSection section)
    {
        var indices = lod.Indices.Value;
        for (var i = 0; i < section.NumFaces * 3; i++)
        {
            var index = indices[i + section.FirstIndex];
            Indices.Add((uint)index);
        }

        foreach (var vertex in lod.Verts)
        {
            var position = vertex.Position * 0.01f;
            var texCoord = vertex.UV;

            Vertices.AddRange(new[] { position.X, position.Z, position.Y, texCoord.U, 1 - texCoord.V });
        }

        var material = section.Material?.Load<UMaterialInterface>();
        if (material is null) return;
        
        var parameters = new CMaterialParams2();
        material.GetParams(parameters, EMaterialFormat.AllLayers);
        if (parameters.TryGetTexture2d(out var texture, "Diffuse"))
        {
            var bitmap = texture.Decode();
            Texture = ImageResult.FromMemory(bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray(), ColorComponents.RedGreenBlue);
        }
    }

    public void Setup()
    {
        Handle = GL.CreateProgram();

        VBO = new Buffer<float>(Vertices.ToArray(), BufferTarget.ArrayBuffer);
        
        VAO = new VertexArray<float>();
        VAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        VAO.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, 5, 3);
        
        EBO = new Buffer<uint>(Indices.ToArray(), BufferTarget.ElementArrayBuffer);

        Shader = new Shader("shader");
        Shader.Use();
    }
    
    public void Render(Camera camera)
    {
        VAO.Bind();

        Shader.Use();
        Shader.SetMatrix4("uTransform", Matrix4.Identity);
        Shader.SetMatrix4("uView", camera.GetViewMatrix());
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        if (Texture is not null)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Texture.Width, Texture.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, Texture.Data);

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
    }
    
    public void Dispose()
    {
        VBO.Dispose();
        EBO.Dispose();
        VAO.Dispose();
        Shader.Dispose();
        GL.DeleteProgram(Handle);
    }
}
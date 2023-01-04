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
using FortnitePorting.Views.Extensions;
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
    public Matrix4 Transform { get; set; }
    
    private List<float> Vertices = new();
    private List<uint> Indices = new();
    private const int VertexSize = 3 + 2 + 3 + 3; // Pos + UV + Normal + Tangent
    
    private Texture? Diffuse;
    private Texture? Normals;
    private Texture? SpecularMasks;
    private Texture? Mask;
    private CubemapTexture? Cubemap;

    public UnrealSection(CSkelMeshLod lod, CMeshSection section, UMaterialInterface? material)
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
            var normal = vertex.Normal;
            var tangent = vertex.Tangent;

            Vertices.AddRange(new[] { 
                position.X, position.Z, position.Y, 
                texCoord.U, 1 - texCoord.V, 
                normal.X, normal.Z, normal.Y,
                tangent.X, tangent.Z, tangent.Y
            });
        }
        
        if (material is null) return;

        var parameters = new CMaterialParams2();
        material.GetParams(parameters, EMaterialFormat.AllLayers);
        
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
        
        var textures = new[] { "px", "nx", "ny", "py", "pz", "nz" };
        Cubemap = new CubemapTexture(textures);
    }

    public void Setup()
    {
        Handle = GL.CreateProgram();

        VBO = new Buffer<float>(Vertices.ToArray(), BufferTarget.ArrayBuffer);
        
        VAO = new VertexArray<float>();
        VAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, VertexSize, 0);
        VAO.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, VertexSize, 3);
        VAO.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, VertexSize, 5);
        VAO.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, VertexSize, 8);
        
        EBO = new Buffer<uint>(Indices.ToArray(), BufferTarget.ElementArrayBuffer);
    }
    
    public void Render(Camera camera, Shader? shader)
    {
        VAO.Bind();

        shader.Use();
        shader.SetMatrix4("uTransform", Matrix4.Identity);
        shader.SetMatrix4("uView", camera.GetViewMatrix());
        shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        shader.SetUniform("diffuseTex", 0);
        shader.SetUniform("normalTex", 1);
        shader.SetUniform("specularTex", 2);
        shader.SetUniform("maskTex", 3);
        shader.SetUniform("cubemap", 4);
        shader.SetUniform3("viewPos", camera.Position);
        
        Diffuse?.Bind(TextureUnit.Texture0);
        Normals?.Bind(TextureUnit.Texture1);
        SpecularMasks?.Bind(TextureUnit.Texture2);
        Mask?.Bind(TextureUnit.Texture3);
        Cubemap?.Bind(TextureUnit.Texture4);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
    }
    
    public void Dispose()
    {
        VBO.Dispose();
        EBO.Dispose();
        VAO.Dispose();
        GL.DeleteProgram(Handle);
    }
}
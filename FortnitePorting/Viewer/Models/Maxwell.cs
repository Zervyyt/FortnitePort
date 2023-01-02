using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using FortnitePorting.Viewer.Buffers;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace FortnitePorting.Viewer.Models;

public class Maxwell : IRenderable
{
    public int Handle { get; set; }
    public Buffer<float> VBO { get; set; }
    public Buffer<uint> EBO { get; set; }
    public VertexArray<float> VAO { get; set; }
    public Shader Shader { get; set; }
    public Matrix4 Transform { get; set; }
    
    private List<float> Vertices = new();
    private List<uint> Indices = new();
    private ImageResult Texture;

    public Maxwell()
    {
        var assimp = new AssimpContext();
        var file = assimp.ImportFile(@"C:\Users\Max\Downloads\maxwell.obj");
        var mesh = file.Meshes[0];
        foreach (var face in mesh.Faces)
        {
            foreach (var index in face.Indices)
            {
                Indices.Add((uint) index);
                var vertex = mesh.Vertices[index]*0.05f;
                var texCoord = mesh.TextureCoordinateChannels[0][index];

                Vertices.AddRange(new []{vertex.X, vertex.Y, vertex.Z, texCoord.X, texCoord.Y});
            }
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

        Texture = ImageResult.FromStream(File.OpenRead(@"C:\Users\Max\Downloads\dingus-the-cat\textures\dingus_nowhiskers.jpg"), ColorComponents.RedGreenBlue);
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Assimp;
using FortnitePorting.Viewer.Buffers;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace FortnitePorting.Viewer.Models;

public class Skybox : IRenderable
{
    public int Handle { get; set; }
    public Buffer<float> VBO { get; set; }
    public Buffer<uint> EBO { get; set; }
    public VertexArray<float> VAO { get; set; }
    public Shader Shader { get; set; }
    public Matrix4 Transform { get; set; }
    
    private float[] Vertices = {
        -0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f,  0.5f, -0.5f,
        0.5f,  0.5f, -0.5f,
        -0.5f,  0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,

        -0.5f, -0.5f,  0.5f,
        0.5f, -0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,
        -0.5f, -0.5f,  0.5f,

        -0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,

        0.5f,  0.5f,  0.5f,
        0.5f,  0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,

        -0.5f, -0.5f, -0.5f,
        0.5f, -0.5f, -0.5f,
        0.5f, -0.5f,  0.5f,
        0.5f, -0.5f,  0.5f,
        -0.5f, -0.5f,  0.5f,
        -0.5f, -0.5f, -0.5f,

        -0.5f,  0.5f, -0.5f,
        0.5f,  0.5f, -0.5f,
        0.5f,  0.5f,  0.5f,
        0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f, -0.5f
    };

    public CubemapTexture Cubemap;

    public Skybox()
    {
    }

    public void Setup()
    {
        Handle = GL.CreateProgram();

        VBO = new Buffer<float>(Vertices, BufferTarget.ArrayBuffer);
        
        VAO = new VertexArray<float>();
        VAO.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, 3, 0);

        Shader = new Shader("skybox");
        Shader.Use();
        
        var textures = new[] { "px", "nx", "ny", "py", "pz", "nz" };
        Cubemap = new CubemapTexture(textures);
    }
    
    public void Render(Camera camera)
    {
        GL.DepthFunc(DepthFunction.Lequal);
        
        VAO.Bind();
        Shader.Use();
        Cubemap.Bind(TextureUnit.Texture0);
        
        Shader.SetMatrix4("uTransform", Matrix4.Identity);
        var viewMatrix = camera.GetViewMatrix();
        viewMatrix.M41 = 0;
        viewMatrix.M42 = 0;
        viewMatrix.M43 = 0;
        Shader.SetMatrix4("uView", viewMatrix);
        Shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
        GL.DepthFunc(DepthFunction.Less);
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
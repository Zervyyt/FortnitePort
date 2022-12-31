using System;
using System.IO;
using System.Windows;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using StbImageSharp;

namespace FortnitePorting.Viewer.Models;

public class Model
{
    private int Handle;

    private int VBO;
    private int EBO;
    private int VAO;
    
    private Shader Shader;

    private int VertexSize = sizeof(float)*5;
    private readonly float[] Vertices =
    {
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
        0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

        -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
    };
    
    private readonly uint[] Indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    public Model()
    {
        Handle = GL.CreateProgram();
        
        VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO); // Array of Vertices
        GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw); // Vertices Into Buffer Memory
            
        VAO = GL.GenVertexArray();
        GL.BindVertexArray(VAO);

        // Position
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexSize, 0);
        GL.EnableVertexAttribArray(0);
        
        // TexCoord
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexSize, sizeof(float)*3);
        GL.EnableVertexAttribArray(1);

        /*EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, BufferUsageHint.StaticDraw);*/

        Shader = new Shader("shader");
        Shader.Use();

        Texture = ImageResult.FromStream(File.OpenRead(@"C:\Users\Max\RiderProjects\FortnitePorting\FortnitePorting\Resources\gany.jpg"), ColorComponents.RedGreenBlue);
    }

    private ImageResult Texture;

    public virtual void Render(Matrix4 transform)
    {
        GL.BindVertexArray(VAO);
        
        Shader.Use();
        Shader.SetMatrix4("uTransform", transform);

        var view = Matrix4.CreateTranslation(0.0f, 0.0f, -3.0f);
        Shader.SetMatrix4("uView", view);
        
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(55.0f), 16f / 9f, 0.1f, 100.0f);
        Shader.SetMatrix4("uProjection", projection);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Texture.Width, Texture.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, Texture.Data);
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }
}
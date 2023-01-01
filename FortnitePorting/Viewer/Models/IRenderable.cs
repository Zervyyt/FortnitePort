using System;
using FortnitePorting.Viewer.Buffers;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Mathematics;

namespace FortnitePorting.Viewer.Models;

public interface IRenderable : IDisposable
{
    public void Render(Camera camera);
    public void Setup();
    public int Handle { get; set; }

    public Buffer<float> VBO { get; set; }
    public Buffer<uint> EBO { get; set; }
    public VertexArray<float> VAO { get; set; }
    public Shader Shader { get; set; }
    
    public Matrix4 Transform { get; set; }
}
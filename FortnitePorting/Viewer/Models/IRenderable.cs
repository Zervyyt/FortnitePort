using System;
using FortnitePorting.Viewer.Buffers;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Mathematics;

namespace FortnitePorting.Viewer.Models;

public interface IRenderable : IDisposable
{
    public void Render(Camera camera);
    public void Setup();
}
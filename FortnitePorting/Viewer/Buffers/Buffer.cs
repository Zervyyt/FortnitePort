using System;
using OpenTK.Graphics.OpenGL;

namespace FortnitePorting.Viewer.Buffers;

public class Buffer<T> : IDisposable where T : unmanaged
{
    private readonly int Handle;
    private T[] Source;
    private BufferTarget Target;
    
    public unsafe Buffer(T[] source, BufferTarget bufferTarget)
    {
        Source = source;
        Target = bufferTarget;
        Handle = GL.GenBuffer();
        Bind();
        GL.BufferData(Target, Source.Length * sizeof(T), Source, BufferUsageHint.StaticDraw);
    }

    public void Bind()
    {
        GL.BindBuffer(Target, Handle);
    }

    public void UnBind()
    {
        GL.BindBuffer(Target, 0);
    }
    
    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}
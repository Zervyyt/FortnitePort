using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using FortnitePorting.Viewer.Models;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using Image = OpenTK.Windowing.Common.Input.Image;

namespace FortnitePorting.Viewer;

public class ModelViewer : GameWindow
{

    private Cube Cube;
    private Cube Cube2;
    private Cube Cube3;
    
    public ModelViewer(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings) : base(gameSettings, nativeSettings)
    {
        
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        CenterWindow();
        LoadIcon();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        
        GL.Enable(EnableCap.DepthTest);

        Cube = new Cube();
        Cube2 = new Cube();
        Cube3 = new Cube();
    }

    private void LoadIcon()
    {
        var image = Application.GetResourceStream(new Uri("/FortnitePorting;component/FortnitePorting-Dark.ico", UriKind.Relative));
        var bitmap = SKBitmap.Decode(image?.Stream);
        Icon = new WindowIcon(new Image(bitmap.Width, bitmap.Height, bitmap.Bytes));
    }

    private double Time;

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        Time += args.Time;
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        {
            var location = Matrix4.CreateTranslation(0, 0, 0);
            var rotation = Matrix4.CreateRotationY((float)Time);
            var scale = Matrix4.CreateScale(MathF.Abs(MathF.Sin((float)Time)));
            Cube.Render(rotation * location * scale);
        }
        
        {
            var location = Matrix4.CreateTranslation(2,0,-2);
            var rotation = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(15))*Matrix4.CreateRotationY((float)Time*5)*Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-25));
            Cube2.Render(rotation*location);
        }
        
        {
            var location = Matrix4.CreateTranslation(-1.75f,0.5f,-1.5f);
            var rotation = Matrix4.CreateRotationY((float)Time*2)*Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(25));
            Cube3.Render(rotation*location);
        }

        SwapBuffers();
    }

    protected override unsafe void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        
        GLFW.SetWindowShouldClose(WindowPtr, true);
        IsVisible = false;
    }
}
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.Utils;
using FortnitePorting.Viewer.Models;
using FortnitePorting.Viewer.Shaders;
using FortnitePorting.Views.Controls;
using OpenTK.Graphics.OpenGL;
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
    public Renderer Renderer;
    public Camera Camera;
    
    public ModelViewer(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings) : base(gameSettings, nativeSettings)
    {
        Renderer = new Renderer();
        Renderer.Setup();
        Camera = new Camera();
        
        Renderer.AddStatic(new Skybox());
    }

    public void LoadAsset(AssetSelectorItem item)
    {
        Title = $"Model Preview - {item.DisplayName}";
        Renderer.Clear();

        var parts = item.Asset.GetOrDefault("BaseCharacterParts", Array.Empty<UObject>());
        if (parts.Length == 0) parts = item.Asset.GetOrDefault("CharacterParts", Array.Empty<UObject>());

        foreach (var part in parts)
        {
            var skeletalMesh = part.Get<USkeletalMesh>("SkeletalMesh");
            Renderer.Add(new UnrealModel(skeletalMesh));
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        CenterWindow();
        LoadIcon();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.FramebufferSrgb);
        
        SetVisibility(true);
    }

    private void LoadIcon()
    {
        var image = Application.GetResourceStream(new Uri("/FortnitePorting;component/FortnitePorting-Dark.ico", UriKind.Relative));
        var bitmap = SKBitmap.Decode(image?.Stream);
        Icon = new WindowIcon(new Image(bitmap.Width, bitmap.Height, bitmap.Bytes));
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        Camera.Speed += e.OffsetY;
        Camera.Speed = Camera.Speed.Clamp(0.5f, 10);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        var delta = e.Delta * Camera.Sensitivity;
        if (MouseState[MouseButton.Right])
        {
            Camera.CalculateDirection(delta.X, delta.Y);
            Cursor = MouseCursor.Empty;
            CursorState = CursorState.Grabbed;
        }
        else
        {
            Cursor = MouseCursor.Default;
            CursorState = CursorState.Normal;
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        var speed = (float) args.Time * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.W))
            Camera.Position += Camera.Direction * speed;
        if (KeyboardState.IsKeyDown(Keys.S))
            Camera.Position -= Camera.Direction * speed;
        if (KeyboardState.IsKeyDown(Keys.A))
            Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * speed;
        if (KeyboardState.IsKeyDown(Keys.D))
            Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * speed;
        if (KeyboardState.IsKeyDown(Keys.E))
            Camera.Position += Camera.Up * speed;
        if (KeyboardState.IsKeyDown(Keys.Q))
            Camera.Position -= Camera.Up * speed;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        Renderer.Render(Camera);

        SwapBuffers();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        
        SetVisibility(false);
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        Camera.AspectRatio = e.Width / (float) e.Height;
    }
    
    private unsafe void SetVisibility(bool open)
    {
        GLFW.SetWindowShouldClose(WindowPtr, !open);
        IsVisible = open; 
    }
}
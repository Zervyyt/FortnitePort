using System.Collections.Generic;
using FortnitePorting.Viewer.Models;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Windowing.Common;

namespace FortnitePorting.Viewer;

public class Renderer
{
    private readonly List<IRenderable> Dynamic = new();
    private readonly List<IRenderable> Static = new();

    private Shader Shader;

    public void Setup()
    {
        Shader = new Shader("shader");
        Shader.Use();
    }

    public void Add(IRenderable renderable)
    {
        renderable.Setup();
        Dynamic.Add(renderable);
    }   
    
    public void AddStatic(IRenderable renderable)
    {
        renderable.Setup();
        Static.Add(renderable);
    }   
    
    public void Clear()
    {
        Dynamic.Clear();
    }   

    public void Render(Camera camera)
    {
        foreach (var renderable in Dynamic)
        {
            renderable.Render(camera, Shader);
        }
        
        foreach (var renderable in Static)
        {
            renderable.Render(camera);
        }
    }
}
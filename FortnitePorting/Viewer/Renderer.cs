using System.Collections.Generic;
using FortnitePorting.Viewer.Models;
using OpenTK.Windowing.Common;

namespace FortnitePorting.Viewer;

public class Renderer
{
    private readonly List<IRenderable> Dynamic = new();
    private readonly List<IRenderable> Static = new();

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
            renderable.Render(camera);
        }
        
        foreach (var renderable in Static)
        {
            renderable.Render(camera);
        }
    }
}
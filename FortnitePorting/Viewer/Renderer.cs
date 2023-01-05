using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Viewer.Models;
using FortnitePorting.Viewer.Shaders;
using OpenTK.Windowing.Common;

namespace FortnitePorting.Viewer;

public class Renderer
{
    private readonly List<IRenderable> Dynamic = new();
    private readonly List<IRenderable> Static = new();
    private readonly List<Material> Materials = new();

    public Skybox Skybox;
    public Shader ModelShader;

    public void Setup()
    {
        ModelShader = new Shader("shader");
        ModelShader.Use();
        
        Skybox = new Skybox();
        Skybox.Setup();
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
        Materials.Clear();
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
        
        Skybox.Render(camera);
    }

    public Material? GetAddMaterial(UMaterialInterface? materialInterface)
    {
        if (materialInterface is null) return null;
        
        var foundMaterial = Materials.FirstOrDefault(mat => mat.Interface == materialInterface);
        if (foundMaterial is not null) return foundMaterial;

        var material = new Material(materialInterface);
        Materials.Add(material);
        return material;
    }
}
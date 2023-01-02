using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.PSK;

namespace FortnitePorting.Viewer.Models;

public class UnrealModel : IRenderable 
{
    private List<UnrealSection> Sections = new();
    public UnrealModel(CSkeletalMesh convertedMesh)
    {
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new UnrealSection(lod, x)));
    }

    public void Setup()
    {
        Sections.ForEach(x=> x.Setup());
    }
    
    public void Render(Camera camera)
    {
        Sections.ForEach(x=> x.Render(camera));
    }
    
    public void Dispose()
    {
        Sections.ForEach(x => x.Dispose());
    }
}
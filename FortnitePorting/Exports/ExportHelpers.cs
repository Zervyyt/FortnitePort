﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using SkiaSharp;

namespace FortnitePorting.Exports;

public static class ExportHelpers
{
    public static List<ExportPart> CharacterParts(IEnumerable<UObject> inputParts, List<ExportMesh> exportMeshes)
    {
        var exportParts = new List<ExportPart>();
        var headMorphType = ECustomHatType.None;
        var headMorphNames = new Dictionary<ECustomHatType, string>();
        FLinearColor? skinColor = null;
        foreach (var part in inputParts)
        {
            var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
            if (skeletalMesh is null) continue;

            if (!skeletalMesh.TryConvert(out var convertedMesh)) continue;
            if (convertedMesh.LODs.Count <= 0) continue;

            var exportPart = new ExportPart();
            exportPart.MeshPath = skeletalMesh.GetPathName();
            Save(skeletalMesh);

            exportPart.NumLods = convertedMesh.LODs.Count;

            var characterPartType = part.GetOrDefault<EFortCustomPartType>("CharacterPartType");
            exportPart.Part = characterPartType.ToString();

            var genderPermitted = part.GetOrDefault("GenderPermitted", EFortCustomGender.Male);
            exportPart.GenderPermitted = genderPermitted;

            if (part.TryGetValue<UObject>(out var additionalData, "AdditionalData"))
            {
                var socketName = additionalData.GetOrDefault<FName?>("AttachSocketName");
                var attachToSocket = part.GetOrDefault("bAttachToSocket", true);
                if (attachToSocket)
                    exportPart.SocketName = socketName?.Text;

                if (additionalData.TryGetValue(out FName hatType, "HatType"))
                {
                    Enum.TryParse(hatType.Text.Replace("ECustomHatType::ECustomHatType_", string.Empty), out headMorphType);
                }

                if (additionalData.ExportType.Equals("CustomCharacterHeadData"))
                {
                    foreach (var type in Enum.GetValues<ECustomHatType>())
                    {
                        if (additionalData.TryGetValue(out FName[] morphNames, type + "MorphTargets"))
                        {
                            headMorphNames[type] = morphNames[0].Text;
                        }
                    }

                    if (additionalData.TryGetValue(out UObject skinColorSwatch, "SkinColorSwatch"))
                    {
                        var colorPairs = skinColorSwatch.GetOrDefault("ColorPairs", Array.Empty<FStructFallback>());
                        var skinColorPair = colorPairs.FirstOrDefault(x => x.Get<FName>("ColorName").Text.Equals("Skin Boost Color and Exponent", StringComparison.OrdinalIgnoreCase));
                        if (skinColorPair is not null) skinColor = skinColorPair.Get<FLinearColor>("ColorValue");
                    }

                }
            }

            var sections = convertedMesh.LODs[0].Sections.Value;
            for (var idx = 0; idx < sections.Length; idx++)
            {
                var section = sections[idx];
                if (section.Material is null) continue;

                if (!section.Material.TryLoad(out var materialObject)) continue;
                if (materialObject is not UMaterialInterface material) continue;

                var exportMaterial = CreateExportMaterial(material, idx);
                exportPart.Materials.Add(exportMaterial);

            }

            if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
            {
                OverrideMaterials(materialOverrides, ref exportPart);
            }

            exportParts.Add(exportPart);
        }

        if (headMorphType != ECustomHatType.None && headMorphNames.ContainsKey(headMorphType))
        {
            var headPart = exportParts.FirstOrDefault(x => x.Part.Equals("Head"));
            if (headPart is not null)
                headPart.MorphName = headMorphNames[headMorphType];
        }

        if (skinColor is not null)
        {
            var bodyPart = exportParts.FirstOrDefault(x => x.Part.Equals("Body"));
            if (bodyPart is not null)
            {
                foreach (var material in bodyPart.Materials)
                {
                    var foundSkinColor = material.Vectors.FirstOrDefault(x => x.Name.Equals("Skin Boost Color And Exponent"));
                    if (foundSkinColor is not null)
                    {
                        foundSkinColor.Value = skinColor.Value;
                    }
                    else
                    {
                        material.Vectors.Add(new VectorParameter("Skin Boost Color And Exponent", skinColor.Value));
                    }
                }
            }
        }

        exportMeshes.AddRange(exportParts);
        return exportParts;
    }

    public static void Weapon(UObject weaponDefinition, List<ExportMesh> exportParts)
    {
        USkeletalMesh? mainSkeletalMesh = null;
        mainSkeletalMesh = weaponDefinition.GetOrDefault("PickupSkeletalMesh", mainSkeletalMesh);
        mainSkeletalMesh = weaponDefinition.GetOrDefault("WeaponMeshOverride", mainSkeletalMesh);
        Mesh(mainSkeletalMesh, exportParts);

        if (mainSkeletalMesh is null)
        {
            weaponDefinition.TryGetValue(out UStaticMesh? mainStaticMesh, "PickupStaticMesh");
            Mesh(mainStaticMesh, exportParts);
        }

        weaponDefinition.TryGetValue(out USkeletalMesh? offHandMesh, "WeaponMeshOffhandOverride");
        Mesh(offHandMesh, exportParts);

        if (exportParts.Count > 0) return;

        // TODO MATERIAL STYLES
        if (weaponDefinition.TryGetValue(out UBlueprintGeneratedClass blueprint, "WeaponActorClass"))
        {
            var defaultObject = blueprint.ClassDefaultObject.Load();
            if (defaultObject.TryGetValue(out UObject weaponMeshData, "WeaponMesh"))
            {
                Mesh(weaponMeshData.GetOrDefault<USkeletalMesh>("SkeletalMesh"), exportParts);
            }

            if (defaultObject.TryGetValue(out UObject leftWeaponMeshData, "LeftHandWeaponMesh"))
            {
                Mesh(leftWeaponMeshData.GetOrDefault<USkeletalMesh>("SkeletalMesh"), exportParts);
            }

            if (exportParts.Count > 0) // successfully exported mesh
                return;
        }
    }

    public static void Mesh<T>(USkeletalMesh? skeletalMesh, List<T> exportParts) where T : ExportMesh, new()
    {
        if (skeletalMesh is null) return;
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return;
        if (convertedMesh.LODs.Count <= 0) return;

        var exportPart = new T();
        exportPart.MeshPath = skeletalMesh.GetPathName();
        Save(skeletalMesh);

        exportPart.NumLods = convertedMesh.LODs.Count;

        var sections = convertedMesh.LODs[0].Sections.Value;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            if (section.Material is null) continue;

            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            var exportMaterial = CreateExportMaterial(material, idx);
            exportPart.Materials.Add(exportMaterial);
        }

        exportParts.Add(exportPart);
    }

    public static void Mesh<T>(UStaticMesh? staticMesh, List<T> exportParts) where T : ExportMesh, new()
    {
        if (staticMesh is null) return;
        if (!staticMesh.TryConvert(out var convertedMesh)) return;
        if (convertedMesh.LODs.Count <= 0) return;

        var exportPart = new T();
        exportPart.MeshPath = staticMesh.GetPathName();
        Save(staticMesh);

        exportPart.NumLods = convertedMesh.LODs.Count;

        var sections = convertedMesh.LODs[0].Sections.Value;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            if (section.Material is null) continue;


            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            var exportMaterial = CreateExportMaterial(material, idx);
            exportPart.Materials.Add(exportMaterial);
        }

        exportParts.Add(exportPart);
    }

    public static void OverrideMaterials(FStructFallback[] overrides, ref ExportPart exportPart)
    {
        foreach (var materialOverride in overrides)
        {
            var overrideMaterial = materialOverride.Get<FSoftObjectPath>("OverrideMaterial");
            if (!overrideMaterial.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            var exportMaterial = CreateExportMaterial<ExportMaterialOverride>(material, materialOverride.Get<int>("MaterialOverrideIndex"));
            exportMaterial.MaterialNameToSwap = materialOverride.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".");

            for (var idx = 0; idx < exportPart.Materials.Count; idx++)
            {
                if (exportMaterial.SlotIndex >= exportPart.Materials.Count) continue;
                if (exportPart.Materials[exportMaterial.SlotIndex].Hash ==
                    exportPart.Materials[idx].Hash)
                {
                    exportPart.OverrideMaterials.Add(exportMaterial with { SlotIndex = idx });
                }
            }
        }
    }

    public static void OverrideMaterials(FStructFallback[] overrides, List<ExportMaterial> exportMaterials)
    {
        foreach (var materialOverride in overrides)
        {
            var overrideMaterial = materialOverride.Get<FSoftObjectPath>("OverrideMaterial");
            if (!overrideMaterial.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            var exportMaterial = CreateExportMaterial<ExportMaterialOverride>(material, materialOverride.Get<int>("MaterialOverrideIndex"));
            exportMaterial.MaterialNameToSwap = materialOverride.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".");

            exportMaterials.Add(exportMaterial);
        }
    }

    public static (List<TextureParameter>, List<ScalarParameter>, List<VectorParameter>) MaterialParameters(UMaterialInstanceConstant materialInstance)
    {
        var textures = new List<TextureParameter>();
        foreach (var parameter in materialInstance.TextureParameterValues)
        {
            if (!parameter.ParameterValue.TryLoad(out UTexture2D texture)) continue;
            textures.Add(new TextureParameter(parameter.ParameterInfo.Name.Text, texture.GetPathName()));
            Save(texture);
        }

        var scalars = new List<ScalarParameter>();
        foreach (var parameter in materialInstance.ScalarParameterValues)
        {
            scalars.Add(new ScalarParameter(parameter.ParameterInfo.Name.Text, parameter.ParameterValue));
        }

        var vectors = new List<VectorParameter>();
        foreach (var parameter in materialInstance.VectorParameterValues)
        {
            if (parameter.ParameterValue is null) continue;
            vectors.Add(new VectorParameter(parameter.ParameterInfo.Name.Text, parameter.ParameterValue.Value));
        }

        if (materialInstance.Parent is UMaterialInstanceConstant materialParent)
        {
            var (parentTextures, parentScalars, parentVectors) = MaterialParameters(materialParent);
            foreach (var parentTexture in parentTextures)
            {
                if (textures.Any(x => x.Name.Equals(parentTexture.Name))) continue;
                textures.Add(parentTexture);
            }

            foreach (var parentScalar in parentScalars)
            {
                if (scalars.Any(x => x.Name.Equals(parentScalar.Name))) continue;
                scalars.Add(parentScalar);
            }

            foreach (var parentVector in parentVectors)
            {
                if (vectors.Any(x => x.Name.Equals(parentVector.Name))) continue;
                vectors.Add(parentVector);
            }
        }

        var parameters = new CMaterialParams2();
        materialInstance.GetParams(parameters, EMaterialFormat.AllLayers);

        if (parameters.TryGetTexture2d(out var specularMasksTexture, CMaterialParams2.SpecularMasks[0]))
        {
            Save(specularMasksTexture);
            textures.Add(new TextureParameter("SpecularMasks", specularMasksTexture.GetPathName()));
        }

        if (parameters.TryGetTexture2d(out var normalsTexture, CMaterialParams2.Normals[0]))
        {
            Save(normalsTexture);
            textures.Add(new TextureParameter("Normals", normalsTexture.GetPathName()));
        }
        return (textures, scalars, vectors);
    }

    public static (List<TextureParameter>, List<ScalarParameter>, List<VectorParameter>) MaterialParameters(UMaterialInterface materialInterface)
    {
        var parameters = new CMaterialParams2();
        materialInterface.GetParams(parameters, EMaterialFormat.AllLayers);

        var textures = new List<TextureParameter>();
        foreach (var (name, value) in parameters.Textures)
        {
            var texture = (UTexture2D) value;
            Save(texture);
            textures.Add(new TextureParameter(name, texture.GetPathName()));
        }
        return (textures, new List<ScalarParameter>(), new List<VectorParameter>());
    }

    public static (List<TextureParameter>, List<ScalarParameter>, List<VectorParameter>) MaterialParametersOverride(FStructFallback data)
    {
        var textures = new List<TextureParameter>();
        foreach (var parameter in data.GetOrDefault("TextureParams", Array.Empty<FStructFallback>()))
        {
            if (!parameter.TryGetValue(out UTexture2D texture, "Value")) continue;
            textures.Add(new TextureParameter(parameter.Get<FName>("ParamName").Text, texture.GetPathName()));
            Save(texture);
        }

        var scalars = new List<ScalarParameter>();
        foreach (var parameter in data.GetOrDefault("FloatParams", Array.Empty<FStructFallback>()))
        {
            var scalar = parameter.Get<float>("Value");
            scalars.Add(new ScalarParameter(parameter.Get<FName>("ParamName").Text, scalar));
        }

        var vectors = new List<VectorParameter>();
        foreach (var parameter in data.GetOrDefault("ColorParams", Array.Empty<FStructFallback>()))
        {
            if (!parameter.TryGetValue(out FLinearColor color, "Value")) continue;
            vectors.Add(new VectorParameter(parameter.Get<FName>("ParamName").Text, color));
        }

        return (textures, scalars, vectors);
    }

    public static ExportMesh? Mesh(UStaticMesh? skeletalMesh)
    {
        return Mesh<ExportMesh>(skeletalMesh);
    }

    public static T? Mesh<T>(UStaticMesh? staticMesh) where T : ExportMesh, new()
    {
        if (staticMesh is null) return null;
        if (!staticMesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportMesh = new T();
        exportMesh.MeshPath = staticMesh.GetPathName();
        Save(staticMesh);

        exportMesh.NumLods = convertedMesh.LODs.Count;

        var sections = convertedMesh.LODs[0].Sections.Value;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            var exportMaterial = CreateExportMaterial(material, idx);
            exportMesh.Materials.Add(exportMaterial);
        }

        return exportMesh;

    }

    public static ExportMesh? Mesh(USkeletalMesh? skeletalMesh)
    {
        return Mesh<ExportMesh>(skeletalMesh);
    }

    public static T? Mesh<T>(USkeletalMesh? skeletalMesh) where T : ExportMesh, new()
    {
        if (skeletalMesh is null) return null;
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportMesh = new T();
        exportMesh.MeshPath = skeletalMesh.GetPathName();
        Save(skeletalMesh);

        exportMesh.NumLods = convertedMesh.LODs.Count;

        var sections = convertedMesh.LODs[0].Sections.Value;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            if (section.Material is null) continue;

            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;

            var exportMaterial = CreateExportMaterial(material, idx);
            exportMesh.Materials.Add(exportMaterial);
        }

        return exportMesh;
    }

    public static void OverrideMeshes(FStructFallback[] overrides, List<ExportMeshOverride> exportMeshes)
    {
        foreach (var meshOverride in overrides)
        {
            var meshToSwap = meshOverride.Get<FSoftObjectPath>("MeshToSwap");
            var meshToOverride = meshOverride.Get<USkeletalMesh>("OverrideMesh");

            var overrideMeshExport = Mesh<ExportMeshOverride>(meshToOverride);
            if (overrideMeshExport is null) continue;

            overrideMeshExport.MeshToSwap = meshToSwap.AssetPathName.Text;
            exportMeshes.Add(overrideMeshExport);
        }
    }

    public static void OverrideParameters(FStructFallback[] overrides, List<ExportMaterialParams> exportParams)
    {
        foreach (var paramData in overrides)
        {
            var exportMaterialParams = new ExportMaterialParams();
            exportMaterialParams.MaterialToAlter = paramData.Get<FSoftObjectPath>("MaterialToAlter").AssetPathName.Text;
            (exportMaterialParams.Textures, exportMaterialParams.Scalars, exportMaterialParams.Vectors) = MaterialParametersOverride(paramData);
            exportMaterialParams.Hash = exportMaterialParams.GetHashCode();
            exportParams.Add(exportMaterialParams);
        }
    }

    public static ExportMaterial CreateExportMaterial(UObject material, int materialIndex = 0)
    {
        return CreateExportMaterial<ExportMaterial>(material, materialIndex);
    }

    public static T CreateExportMaterial<T>(UObject material, int materialIndex = 0) where T : ExportMaterial, new()
    {
        var exportMaterial = new T
        {
            MaterialName = material.Name,
            SlotIndex = materialIndex
        };

        if (material is UMaterialInstanceConstant materialInstance)
        {
            var (textures, scalars, vectors) = MaterialParameters(materialInstance);
            exportMaterial.Textures = textures;
            exportMaterial.Scalars = scalars;
            exportMaterial.Vectors = vectors;
            exportMaterial.IsGlass = IsGlassMaterial(materialInstance);
            exportMaterial.MasterMaterialName = materialInstance.GetLastParent().Name;
        }
        else if (material is UMaterialInterface materialInterface)
        {
            var (textures, scalars, vectors) = MaterialParameters(materialInterface);
            exportMaterial.Textures = textures;
            exportMaterial.Scalars = scalars;
            exportMaterial.Vectors = vectors;
            exportMaterial.IsGlass = IsGlassMaterial(materialInterface);
        }

        exportMaterial.Hash = material.GetPathName().GetHashCode();
        return exportMaterial;
    }

    public static bool IsGlassMaterial(UMaterialInstanceConstant materialInstance)
    {
        var lastParent = materialInstance.GetLastParent();
        var glassMaterialNames = new[]
        {
            "M_MED_Glass_Master",
            "M_MED_Glass_WithDiffuse",
            "M_Valet_Glass_Master",
            "M_MineralPowder_Glass",
            "M_CP_GlassGallery_Master",
            "M_LauchTheBalloon_Microwave_Glass",
            "M_MED_Glass_HighTower",
            "M_OctopusBall",
            "F_MED_SharpFang_Backpack_Glass_Master"
        };

        return glassMaterialNames.Contains(lastParent.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsGlassMaterial(UMaterialInterface material)
    {
        var glassMaterialNames = new[]
        {
            "M_MED_Glass_Master",
            "M_MED_Glass_WithDiffuse",
            "M_Valet_Glass_Master",
            "M_MineralPowder_Glass",
            "M_CP_GlassGallery_Master",
            "M_LauchTheBalloon_Microwave_Glass",
            "M_MED_Glass_HighTower",
            "M_OctopusBall"
        };

        return glassMaterialNames.Contains(material.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static UMaterialInterface GetLastParent(this UMaterialInstanceConstant obj)
    {
        if (obj.Parent is UMaterial material) return material;
        var hasParent = true;
        var activeParent = obj.Parent;
        while (hasParent)
        {
            if (activeParent is UMaterialInstanceConstant materialInstance)
            {
                activeParent = materialInstance.Parent;
            }
            else
            {
                hasParent = false;
            }
        }

        return activeParent as UMaterialInterface;
    }

    public static readonly List<Task> Tasks = new();
    private static readonly ExporterOptions ExportOptions = new()
    {
        Platform = ETexturePlatform.DesktopMobile,
        LodFormat = ELodFormat.AllLods,
        MeshFormat = EMeshFormat.ActorX,
        TextureFormat = ETextureFormat.Png,
        ExportMorphTargets = true,
        SocketFormat = ESocketFormat.Bone,
        MaterialFormat = EMaterialFormat.AllLayersNoRef
    };

    private static bool AllLodsExist(UObject mesh)
    {
        switch (mesh)
        {
            case USkeletalMesh skeletalMesh:
                if (!skeletalMesh.TryConvert(out var convertedSkel)) return false;
                for (var i = 0; i < convertedSkel.LODs.Count; i++)
                {
                    if (!File.Exists(GetExportPath(mesh, "psk", "_LOD" + i)))
                        return false;
                }
                break;
            case UStaticMesh staticMesh:
                if (!staticMesh.TryConvert(out var convertedStatic)) return false;
                for (var i = 0; i < convertedStatic.LODs.Count; i++)
                {
                    if (!File.Exists(GetExportPath(mesh, "pskx", "_LOD" + i)))
                        return false;
                }
                break;
        }

        return true;
    }

    public static void Save(UObject? obj)
    {
        if (obj is null) return;
        Tasks.Add(Task.Run(() =>
        {
            try
            {
                var extraString = string.Empty;
                switch (obj)
                {
                    case USkeletalMesh skeletalMesh:
                    {
                        if (AllLodsExist(skeletalMesh)) return;

                        var exporter = new MeshExporter(skeletalMesh, ExportOptions, false);
                        exporter.TryWriteToDir(App.AssetsFolder, out var label, out var savedFilePath);
                        break;
                    }

                    case UStaticMesh staticMesh:
                    {
                        if (AllLodsExist(staticMesh)) return;

                        var exporter = new MeshExporter(staticMesh, ExportOptions, false);
                        exporter.TryWriteToDir(App.AssetsFolder, out var label, out var savedFilePath);
                        break;
                    }

                    case UTexture2D texture:
                    {
                        var path = GetExportPath(obj, "png");
                        var shouldExport = true; // assume nothing exists yet
                        if (File.Exists(path))
                        {
                            using var existingBitmap = SKBitmap.Decode(path);
                            var firstMip = texture.GetFirstMip();
                            if (existingBitmap is not null && (firstMip?.SizeX > existingBitmap.Width || firstMip?.SizeY > existingBitmap.Height))
                            {

                                extraString += $"{firstMip.SizeX}x{firstMip.SizeY}";
                                shouldExport = true; // update because higher res
                            }
                            else
                            {
                                shouldExport = false; // texture exists and existing resolution is equal
                            }
                        }

                        if (!shouldExport) return;

                        Directory.CreateDirectory(path.Replace('\\', '/').SubstringBeforeLast('/'));

                        using var bitmap = texture.Decode(texture.GetFirstMip());
                        using var data = bitmap?.Encode(SKEncodedImageFormat.Png, 100);

                        if (data is null) return;
                        File.WriteAllBytes(path, data.ToArray());
                        break;
                    }

                    case UAnimSequence animation:
                    {
                        var path = GetExportPath(obj, "psa", "_SEQ0");
                        if (File.Exists(path)) return;

                        var exporter = new AnimExporter(animation, ExportOptions);
                        exporter.TryWriteToDir(App.AssetsFolder, out var label, out var savedFilePath);
                        break;
                    }

                    case USkeleton skeleton:
                    {
                        var path = GetExportPath(obj, "psk");
                        if (File.Exists(path)) return;

                        var exporter = new MeshExporter(skeleton, ExportOptions);
                        exporter.TryWriteToDir(App.AssetsFolder, out var label, out var savedFilePath);
                        break;
                    }
                }

                Log.Information("Exporting {ExportType}: {FileName}{Extra}", obj.ExportType, obj.Name, (extraString == string.Empty ? string.Empty : ", " + extraString));
            }
            catch (IOException e)
            {
                Log.Error("Failed to export {ExportType}: {FileName}", obj.ExportType, obj.Name);
                Log.Error(e.Message);
            }
        }));
    }

    private static string GetExportPath(UObject obj, string ext, string extra = "")
    {
        var path = obj.Owner != null ? obj.Owner.Name : string.Empty;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var finalPath = Path.Combine(App.AssetsFolder.FullName, path) + $"{extra}.{ext.ToLower()}";
        return finalPath;
    }
}
using System.ComponentModel;

namespace FortnitePorting;

public enum EInstallType
{
    [Description("Lohodefocahadefal Ihidefinstahadefallahadefatiohiodefion (Fahadefastehedefer)")]
    Local,

    [Description("Fohodefortnihidefitehedefe Lihidefivehedefe (Slohodefowehedefer)")]
    Live
}

public enum EAssetType
{
    [Description("Outfits")] Outfit,

    [Description("Back Blings")] Backpack,

    [Description("Pickaxes")] Pickaxe,

    [Description("Gliders")] Glider,

    [Description("Pets")] Pet,

    [Description("Weapons")] Weapon,

    [Description("Emotes")] Dance,

    [Description("Vehicles")] Vehicle,
    
    [Description("Galleries")] Gallery,

    [Description("Props")] Prop,

    [Description("Meshes")] Mesh,

    [Description("Music Packs")] Music,
    
    [Description("Toys")] Toy,
}

public enum EFortCustomPartType : byte
{
    Head = 0,
    Body = 1,
    Hat = 2,
    Backpack = 3,
    MiscOrTail = 4,
    Face = 5,
    Gameplay = 6,
    NumTypes = 7
}

public enum ECustomHatType : byte
{
    HeadReplacement,
    Cap,
    Mask,
    Helmet,
    Hat,
    None
}

public enum ERigType
{
    [Description("Dehedefefauhaudefault Rihidefig")] Default,
    [Description("Tahadefasty™ Rihidefig")] Tasty
}

public enum ESortType
{
    [Description("Dehedefefauhaudefault")] Default,
    [Description("Ahadefa-Z")] AZ,
    [Description("Seaheadefeasohodefon")] Season,
    [Description("Rahadefarihidefity")] Rarity,
    [Description("Sehedeferiehiedefies")] Series
}

public enum EUpdateMode
{
    [Description("Stahadefablehedefe")] Stable,
    [Description("Ehedefexpehedeferihidefimehedefentahadefal")] Experimental
}

public enum EFortCustomGender : byte
{
    Invalid = 0,
    Male = 1,
    Female = 2,
    Both = 3
}

public enum EImageType
{
    [Description("PNG (.png)")] PNG,
    [Description("Targa (.tga)")] TGA
}

public enum ETreeItemType
{
    Folder,
    Asset
}

public enum EAnimGender
{
    Male,
    Female
}
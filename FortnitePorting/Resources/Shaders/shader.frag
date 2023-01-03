#version 460 core
#define PI 3.1415926535897932384626433832795
out vec4 FragColor;

in vec3 fPosition;
in vec2 fTexCoord;
in vec3 fNormal;
in vec3 fTangent;

uniform sampler2D diffuseTex;
uniform sampler2D normalTex;
uniform sampler2D specularTex;
uniform sampler2D maskTex;
uniform samplerCube cubemap;
uniform vec3 viewPos;

vec3 samplerToColor(sampler2D tex) 
{
    return texture(tex, fTexCoord).rgb;
}

vec3 calcNormals() 
{
    vec3 normalTexture = samplerToColor(normalTex).rgb;
    vec3 normal = normalTexture * 2.0 - 1.0;

    vec3 tangentVec = normalize(fTangent);
    vec3 normalVec  = normalize(fNormal);
    vec3 binormalVec  = -normalize(cross(normalVec, tangentVec));
    mat3 combined = mat3(tangentVec, binormalVec, normalVec);

    return normalize(combined * normal);
}

vec3 irradiance(vec3 normals)
{
    vec3 irradiance = vec3(0.0);

    vec3 up    = vec3(0.0, 1.0, 0.0);
    vec3 right = normalize(cross(up, normals));
    up         = normalize(cross(normals, right));

    float sampleDelta = 0.25;
    float nrSamples = 0.0;
    for(float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
    {
        for(float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
        {
            // spherical to cartesian (in tangent space)
            vec3 tangentSample = vec3(sin(theta) * cos(phi),  sin(theta) * sin(phi), cos(theta));
            // tangent space to world
            vec3 sampleVec = tangentSample.x * right + tangentSample.y * up + tangentSample.z * normals;

            irradiance += texture(cubemap, sampleVec).rgb * cos(theta) * sin(theta);
            nrSamples++;
        }
    }

    return PI * irradiance * (1.0 / float(nrSamples));
}

float ggx(vec3 N, vec3 H, float a)
{
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom    = a2;
    float denom  = (NdotH2 * (a2 - 1.0) + 1.0);
    denom        = PI * denom * denom;

    return nom / denom;
}

vec3 blendSoftLight(vec3 base, vec3 blend) {
    return mix(
    sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend),
    2.0 * base * blend + base * base * (1.0 - 2.0 * blend),
    step(base, vec3(0.5))
    );
}

vec3 calcLight() 
{
    float lightStrength = 1.0;
    vec3 lightColor = vec3(1.0f, 0.880164, 0.809952) * lightStrength;
    
    vec3 lightDirection = vec3(0.16f, 0.02f, 0.46f) * 5;
    vec3 normals = calcNormals();
    vec3 viewDir = normalize(viewPos - fPosition);
    vec3 reflectDir = reflect(-lightDirection, normals);
    
    float diffuseLight = max(dot(normals, lightDirection), 0);
    vec3 diffuseColor = diffuseLight * lightColor;

    vec3 specularMasks = samplerToColor(specularTex);
    float specularStrength = specularMasks.r;
    float metallicStrength = specularMasks.g;
    float roughnessStrength = specularMasks.b;
    
    vec3 maskTexture = samplerToColor(maskTex);
    vec3 diffuse = samplerToColor(diffuseTex);
    diffuse *= mix(maskTexture.r, 1.0, 0.5);
    diffuse = blendSoftLight(diffuse, vec3(maskTexture.g));

    //vec3 ambientLight = irradiance(normals) * 0.75;
    
    return diffuse;
}

void main()
{
    vec3 result = calcLight();
    FragColor = vec4(result, 1.0);
}
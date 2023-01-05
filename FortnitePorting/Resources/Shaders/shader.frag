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

vec3 calcReflection(vec3 normals)
{
    vec3 I = normalize(fPosition - viewPos);
    vec3 R = reflect(I, normalize(normals));
    return texture(cubemap, R).rgb;
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

vec3 blendSoftLight(vec3 base, vec3 blend) {
    return mix(
    sqrt(base) * (2.0 * blend - 1.0) + 2.0 * base * (1.0 - blend),
    2.0 * base * blend + base * base * (1.0 - 2.0 * blend),
    step(base, vec3(0.5))
    );
}

float distributionGGX(float roughness, float nDotH)
{
    float numerator = pow(roughness, 2);
    float denominator = PI * pow((pow(nDotH, 2) * (numerator - 1)+1), 2);
    return (numerator * numerator) / denominator;
}

float geometryShlickGGX(float k, float NdotV)
{
    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float geometrySmith(float roughness, float nDotV, float nDotL)
{
    float r = (roughness + 1.0f);
    float k = (r * r) / 8.0f;
    
    float ggx1 = geometryShlickGGX(roughness, k);
    float ggx2 = geometryShlickGGX(roughness, k);

    return ggx1 * ggx2;
}

vec3 fresnelSchlick(vec3 diffuseColor, float metallic, float hDotV)
{
    vec3 F0 = vec3(0.04);
    F0 = mix(F0, diffuseColor, metallic);
    return F0 + (1.0 - F0) * pow(clamp(1.0 - hDotV, 0, 1), 5);
}

vec3 calcLight() 
{
    vec3 maskTexture = samplerToColor(maskTex);
    vec3 diffuse = samplerToColor(diffuseTex);

    vec3 normal = calcNormals();
    vec3 specularMasks = samplerToColor(specularTex);
    float specular = specularMasks.r;
    float metallic = specularMasks.g;
    float roughness = specularMasks.b;

    // ambient
    vec3 ambientColor = irradiance(normal);
    
    // light
    vec3 lightDirection = normalize(vec3(0.16f, 0.09f, 0));
    vec3 lightColor = vec3(1.0f, 0.880164, 0.809952);

    // diffuse light
    float diffuseLight = max(dot(normal, lightDirection), 0.0f);
    vec3 diffuseColor = diffuseLight * lightColor;
    
    // specular light
    vec3 l = normalize(lightDirection);
    vec3 v = normalize(viewPos - fPosition);
    vec3 h = normalize(v + l);

    float nDotH = max(dot(normal, h), 0.0);
    float hDotV = max(dot(h, v), 0.0);
    float nDotL = max(dot(normal, l), 0.0);
    float nDotV = max(dot(normal, v), 0.0);
    
    vec3 fresnel = fresnelSchlick(diffuse, metallic, hDotV);
    float brdfNumerator = distributionGGX(roughness, nDotH) * geometrySmith(roughness, nDotV, nDotL);
    float brdfDenominator = 4 * nDotV * nDotL + 0.0001;
    float specularLight = specular * brdfNumerator * brdfDenominator;
    vec3 specularColor = specularLight * lightColor;

    vec3 kd = (1.0f - fresnel) * (1.0f - metallic);
    diffuse *= kd;
    
    //final light
    vec3 finalLighting = diffuse * (ambientColor + diffuseColor);
    
    // fake skin softening
    vec3 result = mix(finalLighting, diffuse, maskTexture.b * 0.3);
    
    // ambient occlusion and cavity
    result *= mix(maskTexture.r, 1.0, 0.5);
    result = blendSoftLight(result, vec3(maskTexture.g));
    
    
    return result;
}

void main()
{
    vec3 result = calcLight();
    FragColor = vec4(result, 1.0);
}
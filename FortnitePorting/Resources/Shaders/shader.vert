#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

out vec2 texCoord;

uniform mat4 uTransform;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = vec4(aPosition, 1.0) * uTransform * uView * uProjection;
    texCoord = aTexCoord;
}
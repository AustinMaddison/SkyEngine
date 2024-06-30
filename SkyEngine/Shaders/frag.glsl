#version 400 core
out vec4 FragColor;
in vec2 TexCoord;

uniform vec2 uResolution;
uniform float uTime;



void main()
{
    vec2 uv = TexCoord * uResolution.xy / max(uResolution.x, uResolution.y);
    vec2 map; 
    map.x = sin(uv.x * 10.0f + uTime) * 0.5f + 0.5f;
    FragColor = vec4(map, 0.0f, 1.0f);
}
     
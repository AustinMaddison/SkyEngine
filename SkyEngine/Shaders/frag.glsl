#version 400 core
out vec4 FragColor;
in vec2 TexCoord;

uniform vec2 uResolution;
uniform float uTime;
uniform vec2 uMousePos;
uniform int uMouseBtnDown;

vec3 pallete(float t)
{
    vec3 a = vec3(0.5f, 0.5f, 0.5f);
    vec3 b = vec3(0.5f, 0.5f, 0.5f);
    vec3 c = vec3(1.0f, 1.0f, 0.5f);
    vec3 d = vec3(0.80f, 0.90f, 0.30f);
    
    return a + b * cos(6.28318f * (c * t + d));
}

void main()
{
    vec2 uv = TexCoord * uResolution.xy / max(uResolution.x, uResolution.y);
    vec2 uv0 = uv;
    
    vec2 map = (uv - 0.5f) * 2.0f;

    
    vec2 mouse = uMousePos * uResolution.xy / max(uResolution.x, uResolution.y);
    mouse = (mouse - 0.5f) * 2.0f;
    
    vec3 finalColor = vec3(0.0f);
    for (float i = 0.0f; i < 2.0f; i += 1.0f)
    {

        map *= 2.0f;
        map = fract(map);
        map -= 0.5f;
        
        float d = length(map - mouse) * exp(-length(map - mouse));

        d += sin(d * 8.f + uTime*8.f) / 8.f;
        d = abs(d);
        d = 0.03 / d;
        
        vec3 col = pallete(length((uv0 - 0.5) * 2.f - mouse) + uTime);
        col *= d;
        
        finalColor += col;
    }
    
    FragColor = vec4(finalColor, 1.0f);
}
     
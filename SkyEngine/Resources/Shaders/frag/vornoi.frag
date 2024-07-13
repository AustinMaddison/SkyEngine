#version 460 core
out vec4 FragColor;
in vec2 TexCoord;

// Uniforms
uniform vec2 uResolution;
uniform vec3 uCloudScale;
uniform float uTime;
uniform vec2 uMousePos;
uniform int uMouseBtnDown;

// Remaps 1d -> 3d color based on map.
vec3 colorize(float t)
{
    vec3 a = vec3(0.8f, 0.5f, 0.9f);
    vec3 b = vec3(0.5f, 0.5f, 0.9f);
    vec3 c = vec3(1.0f, 1.0f, 0.1f);
    vec3 d = vec3(0.80f, 0.90f, 0.30f);
    
    return a + b * cos(6.28318f * (c * t + d));
}


float sdf_sphere(vec3 p, float r)
{
    return length(p) - r;
}

float map(vec3 p)
{
    float d = 0.0f;
//    d += min(sdf_sphere(vec3(  0.5f, 0.0f, 0.0) - p, 0.5f), sdf_sphere(vec3( -0.5f, 0.0f, 0.0) - p, 0.5f));
    
    d += sdf_sphere(p, 0.5f);
    return d;
}

void main()
{
    
    vec2 ratio = uResolution.xy / uResolution.y ;
    vec2 mouse = (uMousePos - 0.5) * ratio;
//    mouse.x *= uResolution.x /uResolution.y ; 
    
    
//    vec2 st = TexCoord * ratio;
//    st.x -= ratio.x * 0.5;
    vec2 uv = (TexCoord - 0.5) * ratio; 
    vec2 st = uv; 
    
//    st *= 4.0;
//    vec2 f_st = fract(st);
//    vec2 i_st = floor(st);
//
    vec2 points[5];
    points[0] = vec2(0.10, 0.9);
    points[1] = vec2(0.9, 0.3);
    points[2] = vec2(0.4, 0.1);
    points[3] = vec2(0.4, 0.8);
    
    float min_dist = 1.0f;
    for(int i = 0; i < 4; i++)
    {
        float dist = distance(st, points[i] - vec2(0.5));
        min_dist = min(min_dist, dist);
    }

    float dist = distance(st, mouse);
    min_dist = min(min_dist, dist);
    
//    min_dist = smoothstep(0.1, 0.3, min_dist);
    
    vec3 col = vec3(0.0f);
    
//    min_dist = smoothstep(0.1, 0., pow(min_dist, 1.5f));
    col += colorize(min_dist);
    
    
//    col = vec3(distance(vec2(st.x), vec2(0,0)) - 1); 
    FragColor = vec4(col, 1.0f);
//    FragColor = vec4(uv, 0.0, 1.0f);
}
     
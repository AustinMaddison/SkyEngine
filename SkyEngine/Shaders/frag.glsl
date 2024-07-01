#version 400 core
out vec4 FragColor;
in vec2 TexCoord;

// Uniforms
uniform vec2 uResolution;
uniform float uTime;
uniform vec2 uMousePos;
uniform int uMouseBtnDown;

// Remaps 1d -> 3d color based on map.
vec3 pallete(float t)
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
    vec2 mouse = uMousePos * uResolution.xy / uResolution.x;
    mouse = (mouse - 0.5f) * 2.0f;
    
    vec2 uv = ( (TexCoord - 0.5)  * 2.f) * uResolution.xy / uResolution.y;

    // raymarching
    vec3 ro = vec3(0.0f, 0.0f, -2.0f);
    vec3 rd = normalize(vec3(uv/0.5, 1.0f)); // translates UV's 2D -> 3D 
    vec3 col = vec3(0.0f);

    float dist = 0.0f;
    for (int i = 0; i < 100; i++)
    {

//        ro += vec3(sin(dist), cos(dist * 0.5f), 0.f)* 0.005f;
        ro.xy += vec2(tan(uTime * 0.25) + cos(uTime), tan(uTime * 0.25) + sin(uTime) ) * 0.9f * ((sin(uTime)) + 1)/10;
//        rd.z += (cos(uTime) + 1) / 2;
        vec3 p = ro + rd * dist;
         
        p.z += uTime* 5.;
//        p.x *= exp(cos(uTime));
//        p.z *= exp(tan(uTime));
        
        p = fract(p * 0.5f) * 2.0f - 1.f;
//        
//        p.y += sin(dist * 5.f);
//        p.x *= cos(dist * 5.f);
//        p += vec3(mouse, 0.f);
        float d = map(p);
        dist += d;
       
        // Stop if we are close enough to the surface or too far away.
        if (d < 0.001f || dist > 100.0f)
            break;
    }
    col = vec3(dist * 0.05);
    col = pallete(dist * .05);
    
    FragColor = vec4(col, 1.0f);
}
     
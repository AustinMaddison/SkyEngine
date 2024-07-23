/*
This is remix by me of the original shader by AndrewHelmer, for atmospheric scattering.
https://www.shadertoy.com/view/slSXRW
Volumetric clouds and light scattering tecniques from Robobo1221
*/

#version 460 core
out vec4 FragColor;
in vec2 TexCoord;

uniform vec2 uResolution;
uniform float uTime;
uniform vec2 uMousePos;

uniform int uCameraMode;

uniform vec4 uCloudFbmScales;
uniform vec4 uCloudFbmWeights;

uniform vec3 uCloudScale;
uniform float uCloudSpeed;
uniform float uCloudHeight;
uniform float uCloudThickness;
uniform float uCloudDensity;

uniform float uFogDensity;
uniform vec3 uRayleighCoeff;
uniform vec3 uMieCoeff;

uniform float uSunBrightness;

uniform float uEarthRadius;

// Increases volumetric clouds resolution
#define volumetricCloudSteps 16			
#define volumetricLightSteps 8			
#define cloudShadowingSteps 12			
#define volumetricLightShadowSteps 4	

uniform sampler2D uNoiseSamp2D;


#define sunPosition vec3(1.0, 1.0, 0.0)


float bayer2(vec2 a){
    a = floor(a);
    return fract( dot(a, vec2(.5, a.y * .75)) );
}

vec2 calculateRaySphereIntersection(vec3 position, vec3 direction, float radius) {
    float PoD = dot(position, direction);
    float radiusSquared = radius * radius;

    float delta = PoD * PoD + radiusSquared - dot(position, position);
    if (delta < 0.0) return vec2(-1.0);
          delta = sqrt(delta);

    return -PoD + vec2(-delta, delta);
}


// Generate dithering pattern for volumetric clouds, effiecient sampling
// https://www.shadertoy.com/view/7sfXDn
#define bayer4(a)   (bayer2( .5*(a))*.25+bayer2(a))
#define bayer8(a)   (bayer4( .5*(a))*.25+bayer2(a))
#define bayer16(a)  (bayer8( .5*(a))*.25+bayer2(a))
#define bayer32(a)  (bayer16(.5*(a))*.25+bayer2(a))
#define bayer64(a)  (bayer32(.5*(a))*.25+bayer2(a))
#define bayer128(a) (bayer64(.5*(a))*.25+bayer2(a))




const float pi = acos(-1.0);
const float rPi = 1.0 / pi;
const float hPi = pi * 0.5;
const float tau = pi * 2.0;
const float rLOG2 = 1.0 / log(2.0);

mat3 rotationMatrix(vec3 axis, float angle)
{
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;
    
    float xx = axis.x * axis.x;
    float yy = axis.y * axis.y;
    float zz = axis.z * axis.z;
    
    float xy = axis.x * axis.y;
    float xz = axis.x * axis.z;
    float zy = axis.z * axis.y;
    
    return mat3(oc * xx + c, oc * xy - axis.z * s, oc * xz + axis.y * s,
                oc * xy + axis.z * s, oc * yy + c, oc * zy - axis.x * s, 
                oc * xz - axis.y * s, oc * zy + axis.x * s, oc * zz + c);
}

struct positionStruct
{
	vec2 texcoord;
    vec2 mousecoord;
    vec3 worldPosition;
    vec3 worldVector;
    vec3 sunVector;
} pos;


// Got from scratch a pixel
vec3 calculateWorldSpacePosition(vec2 p)
{
	p = p * 2.0 - 1.0;
    
    vec3 worldSpacePosition =  vec3(p.x, p.y, 1.0);
    

    
    return worldSpacePosition;
}

void gatherPositions(inout positionStruct pos, vec2 TexCoord, vec2 mouseCoord, vec2 screenResolution)
{
    vec2 ratio = uResolution.xy / uResolution.y ;
    vec2 mouse = uMousePos * ratio;
    
    vec2 uv = TexCoord   * ratio; 

    pos.texcoord = uv ;     

    pos.mousecoord = mouse; 
    
    pos.mousecoord = pos.mousecoord.x < 0.001 ? vec2(0.4, 0.64) : pos.mousecoord;
    
    vec2 rotationAngle = radians(vec2(360.0, 180.0) * pos.mousecoord - vec2(0.0, 90.0));
    
    mat3 rotateH = rotationMatrix(vec3(0.0, 1.0, 0.0), rotationAngle.x);
    mat3 rotateV = rotationMatrix(vec3(1.0, 0.0, 0.0), -rotationAngle.y);
    
    pos.worldPosition = calculateWorldSpacePosition(pos.texcoord);
    
    if (uCameraMode == 0) {
    	pos.worldPosition = rotateH * (rotateV * pos.worldPosition);
        
        // Sun position
    	pos.sunVector = normalize(sunPosition);
    }
    if (uCameraMode == 1) {
    	pos.sunVector = normalize(calculateWorldSpacePosition(pos.mousecoord));
    }
    
    pos.worldVector = normalize(pos.worldPosition);
}


#define d0(x) (abs(x) + 1e-8)
#define d02(x) (abs(x) + 1e-3)

vec3 totalCoeff = (uRayleighCoeff * 1e-5) + uMieCoeff;

vec3 scatter(vec3 coeff, float depth){
	return coeff * depth;
}

vec3 absorb(vec3 coeff, float depth){
	return exp2(scatter(coeff, -depth));
}

float calcParticleThickness(float depth){
   	
    depth = depth * 2.0;
    depth = max(depth + 0.01, 0.01);
    depth = 1.0 / depth;
    
	return 100000.0 * depth;   
}

float calcParticleThicknessH(float depth){
   	
    depth = depth * 2.0 + 0.1;
    depth = max(depth + 0.01, 0.01);
    depth = 1.0 / depth;
    
	return 100000.0 * depth;   
}

float calcParticleThicknessConst(const float depth){
    
	return 100000.0 / max(depth * 2.0 - 0.01, 0.01);   
}

float rayleighPhase(float x){
	return 0.375 * (1.0 + x*x);
}

float hgPhase(float x, float g)
{
    float g2 = g*g;
	return 0.25 * ((1.0 - g2) * pow(1.0 + g2 - 2.0*g*x, -1.5));
}

float miePhaseSky(float x, float depth)
{
 	return hgPhase(x, exp2(-0.000003 * depth));
}

float powder(float od)
{
	return 1.0 - exp2(-od * 2.0);
}

float calculateScatterIntergral(float opticalDepth, float coeff){
    float a = -coeff * rLOG2;
    float b = -1.0 / coeff;
    float c =  1.0 / coeff;

    return exp2(a * opticalDepth) * b + c;
}


vec3 calcAtmosphericScatter(positionStruct pos, out vec3 absorbLight){
    const float ln2 = log(2.0);
    
    float lDotW = dot(pos.sunVector, pos.worldVector);
    float lDotU = dot(pos.sunVector, vec3(0.0, 1.0, 0.0));
    float uDotW = dot(vec3(0.0, 1.0, 0.0), pos.worldVector);
    
	float opticalDepth = calcParticleThickness(uDotW);
    float opticalDepthLight = calcParticleThickness(lDotU);
    
    vec3 scatterView = scatter(totalCoeff, opticalDepth);
    vec3 absorbView = absorb(totalCoeff, opticalDepth);
    
    vec3 scatterLight = scatter(totalCoeff, opticalDepthLight);
         absorbLight = absorb(totalCoeff, opticalDepthLight);
    	 
    vec3 absorbSun = abs(absorbLight - absorbView) / d0((scatterLight - scatterView) * ln2);
    
    vec3 mieScatter = scatter(uMieCoeff, opticalDepth) * miePhaseSky(lDotW, opticalDepth);
    vec3 rayleighScatter = scatter((uRayleighCoeff * 1e-5), opticalDepth) * rayleighPhase(lDotW);
    
    vec3 scatterSun = mieScatter + rayleighScatter;
    
    vec3 sunSpot = smoothstep(0.9999, 0.99993, lDotW) * absorbView * uSunBrightness;
    
    return (scatterSun * absorbSun + sunSpot) * uSunBrightness;
}

vec3 calcAtmosphericScatterTop(positionStruct pos){
    const float ln2 = log(2.0);
    
    float lDotU = dot(pos.sunVector, vec3(0.0, 1.0, 0.0));
    
	float opticalDepth = calcParticleThicknessConst(1.0);
    float opticalDepthLight = calcParticleThickness(lDotU);
    
    vec3 scatterView = scatter(totalCoeff, opticalDepth);
    vec3 absorbView = absorb(totalCoeff, opticalDepth);
    
    vec3 scatterLight = scatter(totalCoeff, opticalDepthLight);
    vec3 absorbLight = absorb(totalCoeff, opticalDepthLight);
    
    vec3 absorbSun = d02(absorbLight - absorbView) / d02((scatterLight - scatterView) * ln2);
    
    vec3 mieScatter = scatter(uMieCoeff, opticalDepth) * 0.25;
    vec3 rayleighScatter = scatter((uRayleighCoeff * 1e-5), opticalDepth) * 0.375;
    
    vec3 scatterSun = mieScatter + rayleighScatter;
    
    return (scatterSun * absorbSun) * uSunBrightness;
}


// https://www.shadertoy.com/view/4dS3Wd
float Get3DNoise(vec3 pos) 
{
    float p = floor(pos.z);
    float f = pos.z - p;
    
    const float invNoiseRes = 1.0 / 64.0;
    
    float zStretch = 17.0 * invNoiseRes;
    
    vec2 coord = pos.xy * invNoiseRes + (p * zStretch);
    
    vec2 noise = vec2(texture(uNoiseSamp2D, coord).x,
					  texture(uNoiseSamp2D, coord + zStretch).x);
    
    return mix(noise.x, noise.y, f);
}

float getClouds(vec3 p)
{
    p = vec3(p.x, length(p + vec3(0.0, uEarthRadius, 0.0)) - uEarthRadius, p.z);
    
    if (p.y < uCloudHeight || p.y > (uCloudThickness + uCloudHeight))
        return 0.0;
    
    float time = uTime * uCloudSpeed;
    vec3 movement = vec3(time, 0.0, time);
    
    vec3 cloudCoord = (p * 0.001) + movement;
    cloudCoord *= uCloudScale;
    
    // Create layered noise
	float noise = Get3DNoise(cloudCoord * uCloudFbmScales.x) * uCloudFbmWeights.x;
    noise += Get3DNoise(cloudCoord * uCloudFbmScales.y + movement) * uCloudFbmWeights.y;
    noise += Get3DNoise(cloudCoord * uCloudFbmScales.z- movement) * uCloudFbmWeights.z;
    noise += Get3DNoise((cloudCoord * uCloudFbmScales.w + movement) * uCloudFbmWeights.w) * 0.0825;
    
    const float top = 0.004;
    const float bottom = 0.01;
    
    float horizonHeight = p.y - uCloudHeight;
    float treshHold = (1.0 - exp2(-bottom * horizonHeight)) * exp2(-top * horizonHeight);
    
    float clouds = smoothstep(0.55, 0.6, noise);
          clouds *= treshHold;
    
    return clouds * uCloudDensity;
}
    
float getCloudShadow(vec3 p, positionStruct pos)
{
	const int steps = volumetricLightShadowSteps;
    float rSteps = uCloudThickness / float(steps) / abs(pos.sunVector.y);
    
    vec3 increment = pos.sunVector * rSteps;
    vec3 position = pos.sunVector * (uCloudHeight - p.y) / pos.sunVector.y + p;
    
    float transmittance = 0.0;
    
    for (int i = 0; i < steps; i++, position += increment)
    {
		transmittance += getClouds(position);
    }
    
    return exp2(-transmittance * rSteps);
}

float getSunVisibility(vec3 p, positionStruct pos)
{
	const int steps = cloudShadowingSteps;
    const float rSteps = uCloudThickness / float(steps);
    
    vec3 increment = pos.sunVector * rSteps;
    vec3 position = increment * 0.5 + p;
    
    float transmittance = 0.0;
    
    for (int i = 0; i < steps; i++, position += increment)
    {
		transmittance += getClouds(position);
    }
    
    return exp2(-transmittance * rSteps);
}


// NVIDIA GPU Gems 2 - Chapter 16. Volumetric Light Scattering as a Post-Process
// https://developer.nvidia.com/gpugems/GPUGems2/gpugems2_chapter16.html    
float phase2Lobes(float x)
{
    const float m = 0.6;
    const float gm = 0.8;
    
	float lobe1 = hgPhase(x, 0.8 * gm);
    float lobe2 = hgPhase(x, -0.5 * gm);
    
    return mix(lobe2, lobe1, m);
}

vec3 getVolumetricCloudsScattering(float opticalDepth, float phase, vec3 p, vec3 sunColor, vec3 skyLight, positionStruct pos)
{
    float intergal = calculateScatterIntergral(opticalDepth, 1.11);
    
    float beersPowder = powder(opticalDepth * log(2.0));
    
	vec3 sunlighting = (sunColor * getSunVisibility(p, pos) * beersPowder) * phase * hPi * uSunBrightness;
    vec3 skylighting = skyLight * 0.25 * rPi;
    
    return (sunlighting + skylighting) * intergal * pi;
}

float getHeightFogOD(float height)
{
	const float falloff = 0.001;
    
    return exp2(-height * falloff) * uFogDensity;
}

vec3 getVolumetricLightScattering(float opticalDepth, float phase, vec3 p, vec3 sunColor, vec3 skyLight, positionStruct pos)
{
    float intergal = calculateScatterIntergral(opticalDepth, 1.11);
    
	vec3 sunlighting = sunColor * phase * hPi * uSunBrightness;
         sunlighting *= getCloudShadow(p, pos);
    vec3 skylighting = skyLight * 0.25 * rPi;
    
    return (sunlighting + skylighting) * intergal * pi;
}



vec3 calculateVolumetricClouds(positionStruct pos, vec3 color, float dither, vec3 sunColor)
{
	const int steps = volumetricCloudSteps;
    const float iSteps = 1.0 / float(steps);
    
    
    float bottomSphere = calculateRaySphereIntersection(vec3(0.0, 1.0, 0.0) * uEarthRadius, pos.worldVector, uEarthRadius + uCloudHeight).y;
    float topSphere = calculateRaySphereIntersection(vec3(0.0, 1.0, 0.0) * uEarthRadius, pos.worldVector, uEarthRadius + (uCloudThickness + uCloudHeight)).y;
    
    vec3 startPosition = pos.worldVector * bottomSphere;
    vec3 endPosition = pos.worldVector * topSphere;
    
    vec3 increment = (endPosition - startPosition) * iSteps;
    vec3 cloudPosition = increment * dither + startPosition;
    
    float stepLength = length(increment);
    
    vec3 scattering = vec3(0.0);
    float transmittance = 1.0;
    
    float lDotW = dot(pos.sunVector, pos.worldVector);
    float phase = phase2Lobes(lDotW);
    
    vec3 skyLight = calcAtmosphericScatterTop(pos);
    
    for (int i = 0; i < steps; i++, cloudPosition += increment)
    {
        float opticalDepth = getClouds(cloudPosition) * stepLength;
        
        if (opticalDepth <= 0.0)
            continue;
        
		scattering += getVolumetricCloudsScattering(opticalDepth, phase, cloudPosition, sunColor, skyLight, pos) * transmittance;
        transmittance *= exp2(-opticalDepth);
    }
    
    return mix(color * transmittance + scattering, color, clamp(length(startPosition) * 0.00001, 0.0, 1.0));
}


// ACES Industry Standard Tonemapping Tonemapping
// https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
vec3 ACESFilm(vec3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return clamp((x*(a*x+b))/(x*(c*x+d)+e), 0.0, 1.0);
}

void main()
{
    gatherPositions(pos, TexCoord, uMousePos.xy, uResolution.xy);
    
    float dither = bayer16(TexCoord);
    
    vec3 lightAbsorb = vec3(0.0);

    vec3 color = vec3(0.0);
         color = calcAtmosphericScatter(pos, lightAbsorb);
         color = calculateVolumetricClouds(pos, color, dither, lightAbsorb);
         
         color = pow(color, vec3(1.0 / 2.2)); // gamma correction
         color = ACESFilm(color * 0.5); // tonemapping

    FragColor = vec4(color, 1.0);
}


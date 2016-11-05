// Global ConstBuffer for transform matrix
cbuffer WorldViewProjBuffer : register(b0)
{
    matrix transformMatrix;
	float3 cameraPosition;
	float Padding1;
};

// Global ConstBuffer for global lights
cbuffer GlobalLightsBuffer : register(b1)
{
    float4 globalAmbient;
	float4 globalDirectional;
	float3 lightDirection;
	float padding2;
};

// Defines of vertices
struct VS_IN 
{
    float4 position : POSITION;
	float3 normal : NORMAL;
    float4 color : COLOR;
};

// Directions are passed as TEXCOORD so they will interpolated throughout
// the triangle, so lightning will be more accurate than per-vertex computation
struct PS_IN 
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
	float3 normal : TEXCOORD0;
	float3 light_dir : TEXCOORD1;
	float3 half_dir : TEXCOORD2;
};

PS_IN  Main(VS_IN  input)
{
    PS_IN  output;
    
    // Change the position vector to be 4 units for proper matrix calculations.
    input.position.w = 1.0f;

    // Calculate the position of the vertex against the world, view, and projection matrices.
    output.position = mul(input.position, transformMatrix);
    
    // Store the input color for the pixel shader to use.
    output.color = input.color;
	
	 // Calculate the normal vector against the world matrix only.
    output.normal = mul(input.normal, (float3x3)transformMatrix);
	
    // Input normal is expected to be in world-space and normalized
    output.normal = input.normal;

	// Set light direction and half-vector between normal and light
	output.light_dir = lightDirection;
	output.half_dir = normalize(output.light_dir + normalize(cameraPosition - mul(input.position, (float3x3)		transformMatrix)));
	
	return output;
}

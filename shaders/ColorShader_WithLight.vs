// Global ConstBuffer for transform matrix
cbuffer WorldViewProjBuffer : register(b0)
{
    matrix transformMatrix;
	Vector3 cameraPosition;
	float Padding1;
};

// Defines of vertices
struct VertexInputType
{
    float4 position : POSITION;
	float3 normal : NORMAL;
    float4 color : COLOR;
};

// Directions are passed as TEXCOORD so they will interpolated throughout
// the triangle, so lightning will be more accurate than per-vertex computation
struct PixelInputType
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
	float3 normal : TEXCOORD0;
	float3 light_dir : TEXCOORD1;
	float3 half_dir : TEXCOORD2;
};

PixelInputType Main(VertexInputType input)
{
    PixelInputType output;
    
    // Change the position vector to be 4 units for proper matrix calculations.
    input.position.w = 1.0f;

    // Calculate the position of the vertex against the world, view, and projection matrices.
    output.position = mul(input.position, transformMatrix);
    
    // Store the input color for the pixel shader to use.
    output.color = input.color;
	
	 // Calculate the normal vector against the world matrix only.
    output.normal = mul(input.normal, (float3x3)objectWorldMatrix);
	
    // Input normal is expected to be in world-space and normalized
    output.normal = input.normal;

	// Set light direction and half-vector between normal and light
	output.light_dir = Direction;
	output.half_dir = normalize(output.light_dir + normalize(cameraPosition - mul(input.position, (float3x3)objectWorldMatrix)));
	
	return output;
}

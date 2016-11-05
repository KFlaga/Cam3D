
// Defines of vertices
struct VS_IN
{
	float4 position : POSITION;
  	float3 normal : NORMAL;
    float4 color : COLOR;
	float2 texCoords : TEXCOORD0;
};

struct PS_IN
{
	float4 position : SV_POSITION;
	float2 texCoords : TEXCOORD0;
};

// Global ConstBuffer for scene transform matrix
cbuffer WorldViewProjBuffer : register(b0)
{
	float4x4 transformMatrix;
};

PS_IN Main(VS_IN input)
{
	PS_IN output;

	// Change the position vector to be 4 units for proper matrix calculations.
	input.position.w = 1.0f;

	// Calculate the position of the vertex against the world, view, and projection matrices.
	output.position = mul(input.position, transformMatrix);

	// Store the input color for the pixel shader to use.
	output.texCoords = input.texCoords;

	return output;
}

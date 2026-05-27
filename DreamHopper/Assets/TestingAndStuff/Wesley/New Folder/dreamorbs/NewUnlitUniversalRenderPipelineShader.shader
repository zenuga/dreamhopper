Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
	Properties
	{
		_BaseColor ("Glow Color", Color) = (1,1,1,1)
		_OrbColor ("Orb Color", Color) = (0.2, 0.5, 1.0, 1)
		_Mouse ("Mouse XY", Vector) = (0.5, 0.5, 0, 0)
		_Intensity ("Intensity", Range(0, 1)) = 0.3
		_OrbSize ("Orb Size", Range(0.01, 0.75)) = 0.2
		_DisplaceStrength ("Displace Strength", Range(0, 1)) = 0.05
		_DisplaceSpeed ("Displace Speed", Range(0, 20)) = 2.0
		_DisplaceFrequency ("Displace Frequency", Range(0, 40)) = 6.0
		_FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.0
		_SpecularPower ("Specular Sharpness", Range(1, 256)) = 64.0
		_SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.8
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"RenderPipeline" = "UniversalPipeline"
		}

		Pass
		{
			Name "Unlit"
			Tags { "LightMode" = "SRPDefaultUnlit" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#define NUM_SAMPLES 16
			#define SPIRAL_PI 9.141592653589793
			#define GOLDEN_RATIO 1.61803398875

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float2 uv         : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float3 positionOS  : TEXCOORD0;
				float4 screenPos   : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				float3 worldPos    : TEXCOORD3;
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _BaseColor;
				half4 _OrbColor;
				float4 _Mouse;
				float _Intensity;
				float _OrbSize;
				float _DisplaceStrength;
				float _DisplaceSpeed;
				float _DisplaceFrequency;
				float _FresnelPower;
				float _SpecularPower;
				float _SpecularIntensity;
			CBUFFER_END

			Varyings vert(Attributes input)
			{
				Varyings output;

				// Ripple displacement along the surface normal
				float wave = sin(_Time.y * _DisplaceSpeed + dot(input.positionOS.xyz, float3(1.0, 1.7, 2.3)) * _DisplaceFrequency)
				           + sin(_Time.y * _DisplaceSpeed * 0.7 + dot(input.positionOS.xyz, float3(2.1, 0.9, 1.4)) * _DisplaceFrequency * 0.8);
				wave = wave * 0.25 + 0.5; // remap to [0, 1]
				input.positionOS.xyz += input.normalOS * wave * _DisplaceStrength;

				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionHCS = positionInputs.positionCS;
				output.positionOS  = input.positionOS.xyz;
				output.screenPos   = ComputeScreenPos(positionInputs.positionCS);
				output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
				output.worldPos    = positionInputs.positionWS;
				return output;
			}

			float rand(float3 co)
			{
				return frac(sin(dot(co, float3(12.9898, 78.233, 91.1743))) * 43758.5453);
			}

			float fetch(float2 uv)
			{
				float2 delta = uv - 0.5.xx;
				return step(0.0, length(delta) - _OrbSize);
			}

			float2 offsetSpiral(float sampleIndex)
			{
				float angle = sampleIndex * 9.0 * SPIRAL_PI * 0.801 * 921.0;
				return float2(cos(angle), sin(angle)) * pow(sampleIndex, 1.0 / GOLDEN_RATIO);
			}

			float linearSmoothTriangle(float value)
			{
				return smoothstep(0.0, 1.0, abs(fmod(value, 2.0) - 1.0));
			}

			half4 frag(Varyings input) : SV_Target
			{
				// Project object-space normal to 2D: front of sphere maps to UV center (0.5, 0.5)
				float3 normal = normalize(input.positionOS);
				float2 uv = normal.xy * 0.5 + 0.5;

				// Screen UV kept only as a stable per-pixel jitter seed
				float2 screenUV = input.screenPos.xy / input.screenPos.w;

				float inverseSamples = 1.0 / NUM_SAMPLES;
				float jitter = inverseSamples * rand(float3(screenUV, fmod(_Time.y, 5.5)));
				float sampleStrength = lerp(0.0, _Intensity, linearSmoothTriangle(uv.y + _Time.y));
				float color0 = 0.0;
				float color1 = 0.0;

				[unroll]
				for (int index = 0; index < NUM_SAMPLES; index++)
				{
					float sampleIndex = index * inverseSamples;
					color0 += fetch(uv + offsetSpiral(sampleIndex) * sampleStrength);
					color1 += fetch(uv + offsetSpiral(sampleIndex + jitter) * sampleStrength);
				}

				// Split comparison in object UV space (X axis of the sphere surface)
				float color = uv.x > saturate(_Mouse.x) ? color1 : color0;

				// Depth: NdotV brightens center, darkens edges (Fresnel); spec adds highlight
				float3 worldNormal = normalize(input.worldNormal);
				float3 viewDir  = normalize(_WorldSpaceCameraPos - input.worldPos);
				float  NdotV    = saturate(dot(worldNormal, viewDir));
				float3 lightDir = normalize(float3(0.3, 0.7, 1.0));
				float3 halfDir  = normalize(viewDir + lightDir);
				float  spec     = pow(saturate(dot(worldNormal, halfDir)), _SpecularPower) * _SpecularIntensity;
				float  depth    = pow(NdotV, _FresnelPower);
				half3  orbDepth = _OrbColor.rgb * depth;

				float finalValue = pow(color * inverseSamples, 2.2);
				half3 finalColor = lerp(orbDepth, _BaseColor.rgb * finalValue, finalValue) + spec;
				return half4(finalColor, _BaseColor.a);
			}
			ENDHLSL
		}
	}
}
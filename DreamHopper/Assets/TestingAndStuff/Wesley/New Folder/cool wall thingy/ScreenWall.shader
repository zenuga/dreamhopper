Shader "Custom/ScreenWall"
{
    // =========================================================================
    // PROPERTIES
    // These show up in the Unity material inspector.
    // The C# scripts (ScreenCameraRenderer, ScreenSkyboxController) write to
    // _CameraRenderCube, _PlayerPos, _ScreenNear, and _ScreenFar automatically
    // at runtime -- you don't need to touch those in the inspector.
    // =========================================================================
    Properties
    {
        // Set at runtime by ScreenCameraRenderer.cs -- the cubemap camera feed.
        _CameraRenderCube ("Cubemap Camera Feed", Cube) = "" {}

        // Set at runtime by ScreenSkyboxController.cs -- the player's world position.
        _PlayerPos ("Player Position", Vector) = (0, 0, 0, 0)

        // At this distance and closer, the view looks like a flat screen (no parallax).
        _ScreenNear ("Screen Near Distance", Float) = 5

        // Beyond this distance the view looks like a window (full parallax).
        _ScreenFar ("Screen Far Distance", Float) = 10

        // How much the view actually flattens when close.
        // 0 = never flattens, 1 = fully flat at Screen Near.
        _FlatStrength ("Flatten Strength", Range(0, 1)) = 1

        // How many scanline pairs fit across the wall's UV height.
        // Higher = more (thinner) lines.
        _ScanlineFrequency ("Scanline Frequency", Float) = 200

        // How dark the scanlines are. 0 = invisible, 1 = very dark.
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.3



        // Color and brightness of the scanline glow (only used when Use Glow is checked).
        _GlowColor ("Glow Color", Color) = (0.2, 0.9, 1.0, 1.0)
        _GlowStrength ("Glow Strength", Range(0, 3)) = 1.0

        // Overall color tint and brightness multiplier for the cubemap image.
        _Tint ("Tint", Color) = (1, 1, 1, 1)



    }

    SubShader
    {
        // Tell URP this is a solid opaque object using the standard forward pass.
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Cull back
        ZWrite On

        Pass
        {
            Name "ScreenWall"
            // UniversalForward = the normal URP lighting pass.
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // shader_feature_local: the glow keyword is stripped from the build if never used.
            // When the [Toggle] checkbox is on, Unity sets _USEGLOW_ON for this material.
            #pragma shader_feature_local _USEGLOW_ON

            // URP's core include gives us helper functions and the camera position.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // imports pre written code

            // -----------------------------------------------------------------
            // GPU-SIDE PROPERTY DECLARATIONS
            // Textures must be declared OUTSIDE the constant buffer.
            // -----------------------------------------------------------------
            TEXTURECUBE(_CameraRenderCube);//declare the cubemap
            SAMPLER(sampler_CameraRenderCube); // instructions on how to read texture

            // Everything else goes in the constant buffer.
            // This is a URP requirement for SRP Batcher compatibility
            // (batching draw calls together for better performance).
            CBUFFER_START(UnityPerMaterial) //constant buffer (dont change during draw )
                float4 _PlayerPos;
                float  _ScreenNear;
                float  _ScreenFar;
                float  _FlatStrength;
                float  _ScanlineFrequency;
                float  _ScanlineStrength;
                float  _UseGlow;
                float4  _GlowColor;
                float  _GlowStrength;
                float4  _Tint;
            CBUFFER_END

            // -----------------------------------------------------------------
            // STRUCT: Attributes
            // Data that comes in from each vertex of the mesh.
            // -----------------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;  // Vertex position in object (local) space
                float3 normalOS   : NORMAL;    // Vertex normal in object space
                float2 uv         : TEXCOORD0; // Mesh UV coordinates
            };

            // -----------------------------------------------------------------
            // STRUCT: Varyings
            // Data that the vertex shader computes per-vertex and sends to the
            // fragment shader. The GPU interpolates these values across the triangle.
            // -----------------------------------------------------------------
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // Clip-space position (required output)
                float3 worldPos    : TEXCOORD0;   // World-space position of this pixel
                float3 worldNormal : TEXCOORD1;   // World-space normal of this pixel
                float2 uv          : TEXCOORD2;   // UV passed through unchanged
            };

            // -----------------------------------------------------------------
            // VERTEX SHADER
            // Runs once per vertex. Its main job is to transform positions
            // and pass data down to the fragment shader.
            // -----------------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // TransformObjectToHClip: converts the vertex from local space
                // through world space into clip space (the final screen position).
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // TransformObjectToWorld: same position but kept in world space,
                // so the fragment shader can measure distances.
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // TransformObjectToWorldNormal: rotates the normal into world space.
                // The normal tells us which direction the wall face is pointing.
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                OUT.uv = IN.uv;
                return OUT;
            }

            // -----------------------------------------------------------------
            // FRAGMENT SHADER
            // Runs once per pixel. Returns the RGBA color for that pixel.
            // -----------------------------------------------------------------
            half4 frag(Varyings IN) : SV_Target
            {
                // =============================================================
                // STEP 1: FIGURE OUT WHAT DIRECTION TO SAMPLE THE CUBEMAP
                //
                // A cubemap is a cube of six textures that together capture
                // a 360-degree view. You sample it by giving a 3D direction vector.
                // The direction determines what part of the captured scene you see.
                // =============================================================

                // PARALLAX (window) direction:
                // "From the camera, in what direction does this point on the wall lie?"
                // Every pixel has a slightly different direction -> parallax effect.
                // Looks like you're peering through a real window into another space.
                float3 viewDir = normalize(IN.worldPos - GetCameraPositionWS());

                // FLAT (screen) direction:
                // "Straight into the wall face, the same for every pixel."
                // -worldNormal = flip the normal so it points INTO the wall, not away.
                // No parallax: every pixel sees the same part of the cubemap -> screen look.
                float3 flatDir = normalize(-IN.worldNormal);

                // =============================================================
                // STEP 2: BLEND BETWEEN WINDOW AND SCREEN BASED ON DISTANCE
                //
                // We want far == window (parallax), close == screen (flat).
                // =============================================================

                // How many units away is the player from this pixel of the wall?
                float dist = distance(_PlayerPos.xyz, IN.worldPos);

                // Map the distance to a 0..1 range:
                // dist >= _ScreenFar  -> rawFlat = 0  (far away, full parallax window)
                // dist <= _ScreenNear -> rawFlat = 1  (very close, fully flat screen)
                // saturate() clamps so nothing goes outside [0, 1].
                float rawFlat = 1.0 - saturate((dist - _ScreenNear) / max(0.001, _ScreenFar - _ScreenNear));

                // _FlatStrength lets the artist dial back how much flattening happens.
                float flatAmount = rawFlat * _FlatStrength;

                // lerp(a, b, t) = a + t * (b - a)
                // At flatAmount = 0: sampleDir = viewDir  (window)
                // At flatAmount = 1: sampleDir = flatDir  (screen)
                float3 sampleDir = normalize(lerp(viewDir, flatDir, flatAmount));

                // =============================================================
                // STEP 3: SAMPLE THE CUBEMAP
                //
                // Look up the color in the cubemap in the direction we calculated.
                // SAMPLE_TEXTURECUBE is URP's macro -- same as texCUBE in old CG.
                // =============================================================
                half4 cubeColor = SAMPLE_TEXTURECUBE(_CameraRenderCube, sampler_CameraRenderCube, sampleDir);
                half3 color = cubeColor.rgb * _Tint.rgb;

                // =============================================================
                // STEP 4: SCANLINES
                //
                // sin() produces a smooth wave going from -1 to 1 and back.
                // * 0.5 + 0.5 remaps it to [0, 1]:  0 = darkest line,  1 = bright gap.
                // Multiplying uv.y by frequency controls how many lines fit vertically.
                // =============================================================
                float scan = sin(IN.uv.y * _ScanlineFrequency * 6.28318) * 0.5 + 0.5;

                // scanDim is a brightness multiplier for the image:
                // lerp(1.0, scan, strength):
                //   strength = 0 -> scanDim = 1.0       (no darkening at all)
                //   strength = 1 -> scanDim = scan value (full lines)
                float scanDim = lerp(1.0, scan, _ScanlineStrength);
                color *= scanDim;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
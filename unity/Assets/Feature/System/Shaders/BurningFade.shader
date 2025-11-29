Shader "UI/BurningFade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0.01, 0.5)) = 0.1
        _NoiseScale ("Noise Scale", Range(1, 20)) = 8
        _BurnColor ("Burn Color", Color) = (1, 0.5, 0, 1)
        _EmberColor ("Ember Color", Color) = (1, 0.2, 0, 1)
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Progress;
            float _EdgeWidth;
            float _NoiseScale;
            float4 _BurnColor;
            float4 _EmberColor;
            float _DistortionStrength;

            // Simplex noise functions
            float3 mod289(float3 x)
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float2 mod289(float2 x)
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float3 permute(float3 x)
            {
                return mod289(((x * 34.0) + 1.0) * x);
            }

            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                       -0.577350269189626, 0.024390243902439);
                float2 i = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);

                float2 i1;
                i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);

                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;

                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                    + i.x + float3(0.0, i1.x, 1.0));

                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy),
                    dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;

                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;

                m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);

                float3 g;
                g.x = a0.x * x0.x + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }

            // Fractal Brownian Motion for more organic noise
            float fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * snoise(uv * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }

                return value;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Generate noise for burn pattern
                float noise = fbm(uv * _NoiseScale + float2(0, _Time.y * 0.5));
                noise = noise * 0.5 + 0.5; // Normalize to 0-1

                // Add upward movement for flames
                float flameNoise = fbm(uv * _NoiseScale * 0.5 + float2(0, _Time.y * 2.0));

                // Calculate burn threshold based on progress
                // Burn from bottom to top
                float burnThreshold = _Progress * 1.5 - 0.25;
                float burnValue = (1.0 - uv.y) + noise * 0.3 + flameNoise * 0.2;

                // Heat distortion
                float2 distortedUV = uv;
                if (_Progress > 0.01)
                {
                    float heatDistort = snoise(uv * 10.0 + _Time.y * 3.0);
                    float distortMask = smoothstep(burnThreshold - 0.3, burnThreshold, burnValue);
                    distortedUV += float2(heatDistort, heatDistort * 0.5) * _DistortionStrength * distortMask;
                }

                // Sample main texture with distortion
                fixed4 col = tex2D(_MainTex, distortedUV);

                // Calculate edge for burning effect
                float edge = smoothstep(burnThreshold - _EdgeWidth, burnThreshold, burnValue);
                float innerEdge = smoothstep(burnThreshold, burnThreshold + _EdgeWidth * 0.5, burnValue);

                // Apply burn colors
                float3 emberGlow = lerp(_EmberColor.rgb, _BurnColor.rgb, innerEdge);
                float glowIntensity = edge * (1.0 - innerEdge);

                // Mix original color with burn effect
                col.rgb = lerp(col.rgb, col.rgb * 0.3 + emberGlow * 2.0, glowIntensity);

                // Add glow/bloom at burn edge
                col.rgb += emberGlow * glowIntensity * 1.5;

                // Fade out burned area
                float burnedOut = step(burnThreshold + _EdgeWidth * 0.3, burnValue);
                col.a *= 1.0 - burnedOut;

                // Add some ember particles effect
                float emberParticle = snoise(uv * 30.0 + float2(_Time.y * 5.0, _Time.y * 8.0));
                emberParticle = smoothstep(0.7, 0.9, emberParticle) * glowIntensity;
                col.rgb += _BurnColor.rgb * emberParticle * 2.0;

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}

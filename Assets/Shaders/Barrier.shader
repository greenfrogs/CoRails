Shader "Barrier"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.125,0.604,0.894,0)
    }

    SubShader
    {
        Blend One One
        ZWrite Off
        Cull Off

        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 screenuv : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 objectPos : TEXCOORD3;
                float4 vertex : SV_POSITION;
                float depth : DEPTH;
                float3 normal : NORMAL;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.screenuv = ((o.vertex.xy / o.vertex.w) + 1) / 2;
                o.screenuv.y = 1 - o.screenuv.y;
                o.depth = mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;

                o.objectPos = v.vertex.xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            sampler2D _CameraDepthNormalsTexture;
            fixed4 _Color;

            float triWave(float t, float offset, float yOffset)
            {
                return saturate(abs(frac(offset + t) * 2 - 1) + yOffset);
            }

            fixed4 texColor(v2f i, float rim)
            {
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                mainTex.r *= triWave(_Time.x * 5, abs(i.objectPos.y) * 2, -0.7) * 6;
                mainTex.g *= saturate(rim) * (sin(_Time.z * 2 + mainTex.b * 5) + 1);
                return mainTex.r * _Color + mainTex.g * (_Color + 0.5);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 glowColor = fixed4(lerp(_Color.rgb, fixed3(1, 1, 1), pow(0.2, 4)), 1);

                fixed4 hexes = texColor(i, 0.2);

                fixed4 col = _Color * _Color.a + glowColor * 1 + hexes;
                return col;
            }
            ENDCG
        }
    }
}
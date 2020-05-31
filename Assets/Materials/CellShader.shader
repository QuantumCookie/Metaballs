Shader "Unlit/CellShader"
{
    Properties
    {
        [HDR]_Color ("Tint", Color) = (1, 1, 1, 1)
        _NoiseStrength ("Noise Strength", Float) = 0.5
        _NoiseTex ("Noise 1", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "noiseSimplex.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD1;
                float4 worldPos : TEXCOORD0;
            };

            float4 _Color;
            float _NoiseStrength;
            sampler2D _NoiseTex;

            v2f vert (appdata v)
            {
                v2f o;
                
                float noise = tex2Dlod(_NoiseTex, float4(v.uv * 3 + _SinTime.x, 0, 0));
                noise = noise * 0.5 + 0.5;
                if(noise < 0.8) noise = 0;
                
                v.vertex.x += snoise(v.vertex.xyz) * _NoiseStrength * noise;
                v.vertex.y += snoise(v.vertex.xyz) * _NoiseStrength * noise;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            
            ENDCG
        }
    }
}

Shader "Unlit/CellShader"
{
    Properties
    {
        [HDR]_Color ("Tint", Color) = (1, 1, 1, 1)
        _NoiseTex ("Noise", 2D) = "white" {}
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
            sampler2D _NoiseTex;

            v2f vert (appdata v)
            {
                v2f o;
                float noise = tex2Dlod(_NoiseTex, float4(v.uv, 0, 0));
                //v.vertex.xy += noise * 100;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}

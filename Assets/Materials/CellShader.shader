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
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            float4 _Color;
            float _NoiseStrength;
            sampler2D _NoiseTex;

            uniform int _MetaballCount;
            uniform float4 _Metaballs[100];
            uniform float4 _Colors[100];

            v2f vert (appdata v)
            {
                v2f o;
                
                float noise = tex2Dlod(_NoiseTex, float4(v.uv * 3 + _SinTime.x, 0, 0));
                noise = noise * 0.5 + 0.5;
                if(noise < 0.8) noise = 0;
                
                //v.vertex.x += snoise(v.vertex.xyz) * _NoiseStrength * noise;
                //v.vertex.y += snoise(v.vertex.xyz) * _NoiseStrength * noise;
                
                /*_MetaballCount = min(_MetaballCount, 100);
            
                for(int i = 0; i < _MetaballCount; i++)
                {
                    float rr = _Metaballs[i].w * _Metaballs[i].w;
                    float3 d = _Metaballs[i].xyz - v.vertex.xyz;
                    float dd = dot(d, d );
                    
                    v.color = (v.color + _Colors[i]) * 0.5f;
                }*/
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float sum = 0;
                float4 col = float4(0, 0, 0, 0);
                
                _MetaballCount = min(_MetaballCount, 100);
            
                for(int x = 0; x < _MetaballCount; x++)
                {
                    float rr = _Metaballs[x].w * _Metaballs[x].w;
                    float3 d = _Metaballs[x].xyz - i.worldPos;
                    float dd = dot(d, d);
                    dd *= dd;
                    float recip = 1 / (10 + dd);
                    
                    sum += recip;
                    col += recip * _Colors[x];
                }
                
                col = col / sum;
                
                return saturate(col);
            }
            
            ENDCG
        }
    }
}

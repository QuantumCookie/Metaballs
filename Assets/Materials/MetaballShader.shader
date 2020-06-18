Shader "Custom/MetaballShader"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1, 1, 1, 1)
        _Color2 ("Color 2", Color) = (1, 1, 1, 1)
        _Color3 ("Color 2", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _NoiseColor ("Noise Color", Color) = (1, 1, 1, 1)
        _Frequency ("Frequency", Float) = 1
        _Speed ("Speed", Float) = 0
        _CellSize ("Cell Size", Float) = 1
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
    }
    
    SubShader
    {
        Tags
        {
           "RenderType" = "Opaque" 
        }
        
        CGPROGRAM
        
        #pragma surface surf Standard noshadow nolightmap vertex:vert
        #include "noiseSimplex.cginc"
        #include "Voronoise.cginc"
        
        struct Input 
        {
            float3 srcPos;
            float3 viewDir;
            float3 worldNormal;
        };
        
        float4 _Color1, _Color2, _Color3;
        float _Metallic;
        float _Smoothness;
        
        float _Frequency;
        float _Speed;
        
        float _CellSize;
        float3 _BorderColor;
        
        float4 _NoiseColor;
        
        uniform int _MetaballCount;
        uniform float4 _Metaballs[30];
        uniform float4 _Colors[30];
        
        float layeredNoise(float3 value)
        {
            float noise = 0;
            
            float octaves = 4;
            float lacunarity = 1.2;
            float persistence = 0.2;
            
            for(int i = 0; i < octaves; i++)
            {
                float val = snoise(value * pow(lacunarity, i)) * pow(persistence, i);
                noise = (noise + val) * 0.5;
            }
        
            return noise;
        
            /*float a = snoise(value);
            float b = snoise(value * 2);
            a = a + b * 0.2f;
            return saturate(a);*/
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        
            o.srcPos = v.vertex;
            
            float3 viewDir = v.vertex.xyz - _WorldSpaceCameraPos;
            float3 worldNormal = mul(unity_ObjectToWorld, v.normal);
            
            float dispNoise = snoise(v.vertex.xyz * _Frequency);
            dispNoise = voronoiNoise(v.vertex.xyz * _Frequency, _CellSize, _BorderColor);
            dispNoise *= 1 - saturate(dot(viewDir, worldNormal));
            
            //dispNoise = layeredNoise(v.vertex.xyz * _Frequency);
            //dispNoise = abs(dispNoise);
            
            v.vertex = float4(v.vertex.xyz + v.normal * dispNoise * 0.5, v.vertex.w);
            //o.srcPos += _Time.y * _Speed;
        
            /*_MetaballCount = min(_MetaballCount, 30);
            
            float3 pos = v.vertex.xyz;
            float4 color = float4(0, 0, 0, 0);
            float weightSum = 0;
            
            for(int i = 0; i < _MetaballCount; i++)
            {
                float r = _Metaballs[i].w;
                float d = distance(pos, _Metaballs[i].xyz);
                
                if(d < r)
                {
                    color = _Colors[i];
                    v.color = color;
                    return;
                }
                
                float invD = 1 / (1 + d);
                
                color += _Colors[i] * invD;
                weightSum += invD;
            }
            
            color /= weightSum;
            saturate(color);*/
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float noise = snoise(IN.srcPos * _Frequency) * 0.5 + 0.5;
            noise = layeredNoise(IN.srcPos * _Frequency) * 0.5 + 0.5;
            
            noise = voronoiNoise(IN.srcPos * _Frequency, _CellSize, _BorderColor);

			/*float3 cellColor = value.z;//rand1dTo3d(value.y); 
			float valueChange = fwidth(value.z) * 0.5;
			float isBorder = 1 - smoothstep(0.05 - valueChange, 0.05 + valueChange, value.z);
			float3 c = lerp(cellColor, _BorderColor, isBorder);*/
            
            noise *= saturate(dot(IN.viewDir, IN.worldNormal));
        
            float4 color = lerp(_Color1, _Color2, noise);
            color = lerp(color, _Color3, noise);
            color *= noise;
        
            o.Albedo = color;
            o.Emission = color;
            //o.Albedo = abs(sin(80 * dot(IN.worldNormal, float3(-1, -1, -1))));
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }
        
        ENDCG
    }
    
    Fallback "Standard"
}

/*Shader "Custom/MetaballShader"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        _FresnelIntensity ("Fresnel Intensity", Range(0.1, 10)) = 1
    }
    
    SubShader
    {
        Tags {"RenderType" = "Opaque"}
            
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
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                
                return o;
            }
            
            float _FresnelIntensity;
            float4 _Color;
            
            float4 frag(v2f v) : SV_Target
            {
                float3 fragPos = v.vertex.xyz;
                float3 camPos = _WorldSpaceCameraPos;
                
                float3 viewDir = fragPos - camPos;
                
                return _Color;
                
                return float4(v.normal, 0);
            }
            
            ENDCG
        }
    }
}*/
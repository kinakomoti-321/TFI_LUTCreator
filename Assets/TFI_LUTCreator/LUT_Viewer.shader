Shader "TFI_LUTCreator/LUT_Viewer"
{
    Properties
    {
        _MiddleLayerThickness("ThinFilm_length",Float) = 550
        _MiddleLayerMinimamThickness("Minimam Thickness",Float) = 200
        _MiddleLayerMaximamThickness("Maximam Thickness",Float) = 1000
        _LUT("LUT",2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            #define M_PI 3.14159265359

            float _MiddleLayerMaximamThickness;
            float _MiddleLayerMinimamThickness;
            float _MiddleLayerThickness;
            sampler2D _LUT;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldpos : TEXCOORD2;
                float3 eyeDir : TEXCOORD3;
            };

            float square(float a){
                return a * a;
            }
            float3 square(float3 a){
                return a * a;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldpos = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.eyeDir = normalize(WorldSpaceViewDir(v.vertex));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float thickness = (_MiddleLayerThickness - _MiddleLayerMinimamThickness) / (_MiddleLayerMaximamThickness - _MiddleLayerMinimamThickness);
                thickness = clamp(thickness,0.0,1.0);
                float3 eyeDir = normalize(_WorldSpaceCameraPos - i.worldpos);
                float ndotv = dot(eyeDir,i.normal);
                float2 LUTuv;
                LUTuv.y = thickness;
                LUTuv.x = ndotv;
                float3 F = tex2D(_LUT, LUTuv).xyz;

                float3 refDir = reflect(-eyeDir,i.normal); 

                float3 col = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,refDir);
                return fixed4(F * col,1.0);
            }
            ENDCG
        }
    }
}
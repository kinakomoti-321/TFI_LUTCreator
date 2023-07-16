Shader "TFI_LUTCreator/InterferenceLUT"
{
    Properties
    {
        _TopLayerIOR("IOR of Top Layer, ",Float) = 1.0
        _MiddleLayerIOR("IOR of MiddleLayer",Float) = 1.33
        _MiddleLayerMinimamThickness("Minimam Thickness of Middle Layer",Float) = 200
        _MiddleLayerMaximamThickness("Maximam Thickness of Middle Layer",Float) = 1000
        _BottomLayerIOR("Ior of Bottom Layer",Vector) = (1.5,1.5,1.5,1.5)
        _BottomLayerKappa("Extinction coefficient of Bottom Layer ",Vector) = (1.5,1.5,1.5,1.5)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            #define M_PI 3.14159265359

            float _TopLayerIOR;
            float _MiddleLayerIOR;
            float _MiddleLayerMinimamThickness;
            float _MiddleLayerMaximamThickness;
            float3 _BottomLayerIOR;
            float3 _BottomLayerKappa;
            float3 _BottomLayerBaseColor;
            float3 _BottomLayerEdgeTint;

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

            //https://jcgt.org/published/0003/04/03/paper.pdf
            float3 n_min(float3 r){
                return (1.0 - r) / (1.0 + r);
            }
            float3 n_max(float3 r){
                return (1.0 + sqrt(r)) / (1.0 - sqrt(r));
            }

            float3 colorToIOR(float3 baseColor,float3 edgeTint){
                return n_min(baseColor) * edgeTint + (1.0 - edgeTint) * n_max(baseColor); 
            }
        
            float3 colorToKappa(float3 baseColor,float3 ior){
                float3 nr = (ior + 1.0) * (ior + 1.0) * baseColor - (ior - 1.0) * (ior - 1.0); 
                return sqrt(nr/(1.0 - baseColor));
            }
            
            //https://github.com/AcademySoftwareFoundation/MaterialX/blob/main/libraries/pbrlib/genglsl/lib/mx_microfacet_specular.glsl
            void fresnel_dielectric_polarized(float cosTheta, float n, inout float Rp, inout float Rs)
            {
                if (cosTheta < 0.0) {
                    Rp = 1.0;
                    Rs = 1.0;
                    return;
                }

                float cosTheta2 = cosTheta * cosTheta;
                float sinTheta2 = 1.0 - cosTheta2;
                float n2 = n * n;

                float t0 = n2 - sinTheta2;
                float a2plusb2 = sqrt(t0 * t0);
                float t1 = a2plusb2 + cosTheta2;
                float a = sqrt(max(0.5 * (a2plusb2 + t0), 0.0));
                float t2 = 2.0 * a * cosTheta;
                Rs = (t1 - t2) / (t1 + t2);

                float t3 = cosTheta2 * a2plusb2 + sinTheta2 * sinTheta2;
                float t4 = t2 * sinTheta2;
                Rp = Rs * (t3 - t4) / (t3 + t4);
            }

            void fresnel_conductor_polarized(float cosTheta, float3 n, float3 k, inout float3 Rp, inout float3 Rs)
            {
                cosTheta = clamp(cosTheta, 0.0, 1.0);
                float cosTheta2 = cosTheta * cosTheta;
                float sinTheta2 = 1.0 - cosTheta2;
                float3 n2 = n * n;
                float3 k2 = k * k;

                float3 t0 = n2 - k2 - sinTheta2;
                float3 a2plusb2 = sqrt(t0 * t0 + 4.0 * n2 * k2);
                float3 t1 = a2plusb2 + cosTheta2;
                float3 a = sqrt(max(0.5 * (a2plusb2 + t0), 0.0));
                float3 t2 = 2.0 * a * cosTheta;
                Rs = (t1 - t2) / (t1 + t2);

                float3 t3 = cosTheta2 * a2plusb2 + sinTheta2 * sinTheta2;
                float3 t4 = t2 * sinTheta2;
                Rp = Rs * (t3 - t4) / (t3 + t4);
            }

            void fresnel_conductor(float cosTheta,float ior1,float3 ior2,float3 kappa2,inout float3 Rp, inout float3 Rs){
                float3 n = ior2 / ior1;
                float3 k = kappa2 / ior1; 
                fresnel_conductor_polarized(cosTheta,n,k,Rp,Rs);
            }

            void fresnel_dielectric(float cosTheta, float ior1,float ior2, inout float Rp, inout float Rs){
                float n = ior2 /ior1; 
                fresnel_dielectric_polarized(cosTheta,n,Rp,Rs);
            }

            void dielectric_phase_polarized(float cosTheta, float eta1, float eta2, inout float phiP, inout float phiS)
            {
                float cosB = cos(atan(eta2 / eta1));    // Brewster's angle
                if (eta2 > eta1) {
                    phiP = cosTheta < cosB ? M_PI : 0.0f;
                    phiS = 0.0f;
                } else {
                    phiP = cosTheta < cosB ? 0.0f : M_PI;
                    phiS = M_PI;
                }
            }

            void conductor_phase_polarized(float cosTheta, float eta1, float3 eta2, float3 kappa2, inout float3 phiP,inout float3 phiS)
            {
                if (length(kappa2) == 0.0 && eta2.x == eta2.y && eta2.y == eta2.z) {
                    dielectric_phase_polarized(cosTheta, eta1, eta2.x, phiP.x, phiS.x);
                    phiP = phiP.xxx;
                    phiS = phiS.xxx;
                    return;
                }

                float3 k2 = kappa2 / eta2;
                float3 sinThetaSqr = 1.0 - cosTheta * cosTheta;
                float3 A = eta2*eta2*(1.0-k2*k2) - eta1*eta1*sinThetaSqr;
                float3 B = sqrt(A*A + square(2.0*eta2*eta2*k2));
                float3 U = sqrt((A+B)/2.0);
                float3 V = max(0.0, sqrt((B-A)/2.0));

                phiS = atan2(2.0*eta1*V*cosTheta, U*U + V*V - square(eta1*cosTheta));
                phiP = atan2(2.0*eta1*eta2*eta2*cosTheta * (2.0*k2*U - (1.0-k2*k2) * V),
                            square(eta2*eta2*(1.0+k2*k2)*cosTheta) - eta1*eta1*(U*U+V*V));
            }

            float3 eval_sensitivity(float opd, float3 shift)
            {
                float phase = 2.0*M_PI * opd;
                float3 val = float3(5.4856e-13, 4.4201e-13, 5.2481e-13);
                float3 pos = float3(1.6810e+06, 1.7953e+06, 2.2084e+06);
                float3 var = float3(4.3278e+09, 9.3046e+09, 6.6121e+09);
                float3 xyz = val * sqrt(2.0*M_PI * var) * cos(pos * phase + shift) * exp(- var * phase*phase);
                xyz.x   += 9.7470e-14 * sqrt(2.0*M_PI * 4.5282e+09) * cos(2.2399e+06 * phase + shift[0]) * exp(- 4.5282e+09 * phase*phase);
                return xyz / 1.0685e-7;
            }

            float3 fresnel_airy(float cosTheta, float3 ior, float3 extinction, float tf_thickness,float top_ior, float tf_ior){
                float d = tf_thickness * 1e-9; 

                float eta1 = top_ior; //vaccume
                float eta2 = tf_ior; //thin-film ior
                float3 eta3 = ior; //bottom layer ior
                float3 kappa3 = extinction; // bottom layer kappa

                float R12p, T121p, R12s, T121s;
                float3 R23p, R23s;
                
                fresnel_dielectric(cosTheta, eta1, eta2, R12p, R12s);

                float scale = eta1 / eta2;
                float cosThetaTSqr = 1.0 - (1.0-cosTheta*cosTheta) * scale*scale;
                float cosTheta2 = sqrt(cosThetaTSqr);

                fresnel_conductor(cosTheta2, eta2, eta3, kappa3, R23p, R23s);

                // Compute the transmission coefficients
                T121p = 1.0 - R12p;
                T121s = 1.0 - R12s;

                // Optical path difference
                float D = 2.0 * eta2 * d * cosTheta2;
                float3 Dphi = 2.0 * M_PI * D / float3(580.0, 550.0, 450.0);

                float phi21p, phi21s;
                float3 phi23p, phi23s, r123s, r123p;

                // Evaluate the phase shift
                dielectric_phase_polarized(cosTheta, eta1, eta2, phi21p, phi21s);
                conductor_phase_polarized(cosTheta2, eta2, eta3, kappa3, phi23p, phi23s);

                phi21p = M_PI - phi21p;
                phi21s = M_PI - phi21s;

                r123p = max(0.0, sqrt(R12p*R23p));
                r123s = max(0.0, sqrt(R12s*R23s));

                // Evaluate iridescence term
                float3 I = 0.0;
                float3 C0, Cm, Sm;

                // Iridescence term using spectral antialiasing for Parallel polarization

                float3 S0 = 1.0;

                // Reflectance term for m=0 (DC term amplitude)
                float3 Rs = (T121p*T121p*R23p) / (1.0 - R12p*R23p);
                C0 = R12p + Rs;
                I += C0 * S0;

                // Reflectance term for m>0 (pairs of diracs)
                Cm = Rs - T121p;
                for (int m=1; m<=2; ++m)
                {
                    Cm *= r123p;
                    Sm  = 2.0 * eval_sensitivity(float(m)*D, float(m)*(phi23p+phi21p));
                    I  += Cm*Sm;
                }

                // Iridescence term using spectral antialiasing for Perpendicular polarization

                // Reflectance term for m=0 (DC term amplitude)
                float3 Rp = (T121s*T121s*R23s) / (1.0 - R12s*R23s);
                C0 = R12s + Rp;
                I += C0 * S0;

                // Reflectance term for m>0 (pairs of diracs)
                Cm = Rp - T121s ;
                for (int m=1; m<=2; ++m)
                {
                    Cm *= r123s;
                    Sm  = 2.0 * eval_sensitivity(float(m)*D, float(m)*(phi23s+phi21s));
                    I  += Cm * Sm;
                }

                // Average parallel and perpendicular polarization
                I *= 0.5;

                // Convert back to RGB reflectance
                float3 RGB;
                RGB.r = 2.3706743 * I.x + -0.9000405 * I.y -0.4706338* I.z;
                RGB.g = -0.5138850 * I.x+ 1.4253036* I.y + 0.0885814* I.z;
                RGB.b = 0.0052982 * I.x+ -0.0146949 * I.y+ 1.0093968* I.z;

                return RGB;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float ndotv = dot(i.eyeDir,i.normal);
                ndotv = i.uv.x;
                float thickness = lerp(_MiddleLayerMinimamThickness,_MiddleLayerMaximamThickness,i.uv.y);
                _BottomLayerBaseColor = clamp(_BottomLayerBaseColor,0.0,0.999);
                _BottomLayerEdgeTint = clamp(_BottomLayerEdgeTint,0.0,0.999);
                // float3 bIOR = colorToIOR(_BottomLayerBaseColor,_BottomLayerEdgeTint);
                // float3 bKappa = colorToKappa(_BottomLayerBaseColor,bIOR);
                float3 bIOR = _BottomLayerIOR;
                float3 bKappa = _BottomLayerKappa;
                float3 F = fresnel_airy(ndotv,bIOR,bKappa,thickness,_TopLayerIOR,_MiddleLayerIOR);

                return fixed4(F,1.0);
            }
            ENDCG
        }
    }
}
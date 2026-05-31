Shader "Custom/KopfVordergrund"
{
    Properties
    {
        _MainTex        ("Sprite Texture",  2D)     = "white" {}
        _Color          ("Tint",            Color)  = (1,1,1,1)
        _BumpMap        ("Normal Map",      2D)     = "bump" {}
        _BumpScale      ("Normal Strength", Float)  = 1.0
        _SpecColor      ("Specular Color",  Color)  = (1,1,1,1)
        _Shininess      ("Shininess",       Range(1,256)) = 32
        _SpecIntensity  ("Specular Intensity", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BumpMap;
            fixed4    _Color;
            float     _BumpScale;
            fixed4    _SpecColor;
            float     _Shininess;
            float     _SpecIntensity;

            struct appdata
            {
                float4 vertex  : POSITION;
                float2 uv      : TEXCOORD0;
                fixed4 color   : COLOR;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
                // TBN-Matrix Zeilen fuer Tangent-Space-Beleuchtung
                float3 tbn0     : TEXCOORD1;
                float3 tbn1     : TEXCOORD2;
                float3 tbn2     : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                o.color    = v.color * _Color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // TBN-Matrix (Tangent, Bitangent, Normal) in World Space
                float3 worldNormal  = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBitan   = cross(worldNormal, worldTangent)
                                      * v.tangent.w * unity_WorldTransformParams.w;

                o.tbn0 = worldTangent;
                o.tbn1 = worldBitan;
                o.tbn2 = worldNormal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Albedo
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip(col.a - 0.01);

                // Normal Map → Tangent Space → World Space
                float3 normalTS = UnpackNormal(tex2D(_BumpMap, i.uv));
                normalTS.xy    *= _BumpScale;
                float3 normalWS = normalize(
                    normalTS.x * i.tbn0 +
                    normalTS.y * i.tbn1 +
                    normalTS.z * i.tbn2
                );

                // Hauptlichtrichtung (erster Directional Light)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Blinn-Phong Specular
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir  = normalize(lightDir + viewDir);
                float  spec     = pow(max(dot(normalWS, halfDir), 0.0), _Shininess);
                fixed4 specular = _SpecColor * spec * _SpecIntensity;

                col.rgb += specular.rgb;
                return col;
            }
            ENDCG
        }
    }
}

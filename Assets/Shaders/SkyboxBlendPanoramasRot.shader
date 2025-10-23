Shader "Skybox/BlendPanoramasRot"
{
    Properties
    {
        _MainTexDay   ("Day Panorama (Equirect)", 2D) = "white" {}
        _MainTexNight ("Night Panorama (Equirect)", 2D) = "black" {}
        _Exposure     ("Exposure", Range(0,5)) = 1.0
        _Blend        ("Blend Night (0=day,1=night)", Range(0,1)) = 0
        _Tint         ("Tint", Color) = (1,1,1,1)
        _YawDayDeg    ("Day Yaw (deg)", Range(0,360)) = 0
        _YawNightDeg  ("Night Yaw (deg)", Range(0,360)) = 0
    }
    SubShader
    {
        Tags{ "Queue"="Background" "RenderType"="Background" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTexDay;
            sampler2D _MainTexNight;
            float _Exposure, _Blend;
            float4 _Tint;
            float _YawDayDeg, _YawNightDeg;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul((float3x3)UNITY_MATRIX_MV, v.vertex.xyz);
                return o;
            }

            float2 DirToEquirectUV(float3 d, float yawDeg)
            {
                d = normalize(d);
                float a = radians(yawDeg);
                float s = sin(a), c = cos(a);
                // rotacja yaw
                float3 r = float3(c*d.x - s*d.z, d.y, s*d.x + c*d.z);

                float phi = atan2(r.z, r.x);           // -PI..PI
                float theta = acos(saturate(r.y));     // 0..PI
                float2 uv;
                uv.x = phi / (2.0 * UNITY_PI) + 0.5;   // 0..1
                uv.y = theta / UNITY_PI;               // 0..1
                return uv;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                float2 uvDay   = DirToEquirectUV(dir, _YawDayDeg);
                float2 uvNight = DirToEquirectUV(dir, _YawNightDeg);

                fixed4 dayCol   = tex2D(_MainTexDay,   uvDay)   * _Exposure;
                fixed4 nightCol = tex2D(_MainTexNight, uvNight);

                fixed4 col = lerp(dayCol, nightCol, saturate(_Blend)) * _Tint;
                col.rgb = 1.0 - exp(-col.rgb); // prosty tonemapping
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}


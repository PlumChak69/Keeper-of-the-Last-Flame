Shader "Skybox/BlendCubemaps"
{
    Properties
    {
        _CubeDay   ("Day Cubemap", Cube) = "" {}
        _CubeNight ("Night Cubemap", Cube) = "" {}
        _Exposure  ("Exposure", Range(0.0, 5.0)) = 1.0
        _Blend     ("Blend Night (0=day,1=night)", Range(0,1)) = 0
        _Tint      ("Tint", Color) = (1,1,1,1)

        _RotDayDeg   ("Day Y Rotation (deg)",   Range(0,360)) = 0
        _RotNightDeg ("Night Y Rotation (deg)", Range(0,360)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _CubeDay;
            samplerCUBE _CubeNight;
            float _Exposure;
            float _Blend;
            float4 _Tint;
            float _RotDayDeg;
            float _RotNightDeg;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul((float3x3)UNITY_MATRIX_MV, v.vertex.xyz);
                return o;
            }

            float3 RotateY(float3 d, float deg)
            {
                float a = radians(deg);
                float s = sin(a), c = cos(a);
                return float3(c*d.x - s*d.z, d.y, s*d.x + c*d.z);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                // osobna rotacja dla dnia i nocy
                float3 dirDay   = RotateY(dir, _RotDayDeg);
                float3 dirNight = RotateY(dir, _RotNightDeg);

                fixed4 dayCol   = texCUBE(_CubeDay,   dirDay)   * _Exposure;
                fixed4 nightCol = texCUBE(_CubeNight, dirNight);

                fixed4 col = lerp(dayCol, nightCol, saturate(_Blend)) * _Tint;
                col.rgb = 1.0 - exp(-col.rgb); // prosty tonemapping
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}

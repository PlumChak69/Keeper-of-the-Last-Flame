Shader "Unlit/MoonPhase"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Phase ("Phase (0=new, 0.5=full, 1=new)", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Phase;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * _Color;

                // generuj maskê fazy ksiê¿yca (okr¹g z cieniem)
                float2 uv = i.uv * 2.0 - 1.0; 
                float r = length(uv);
                if (r > 1.0) discard; // poza dyskiem

                // pozycja cienia
                float shadow = smoothstep(_Phase - 0.5, _Phase + 0.5, uv.x);
                tex.rgb *= shadow;

                return tex;
            }
            ENDCG
        }
    }
}

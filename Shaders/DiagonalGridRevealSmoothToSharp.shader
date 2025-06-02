
Shader "UI/DiagonalGridRevealSmoothToSharp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Reveal Progress", Float) = 0
        _GridSize ("Grid Size (px)", Float) = 64
        _CornerRadius ("Max Corner Radius", Float) = 0.2
        _ColorA ("Hidden Color", Color) = (0, 0, 0, 1)
        _ColorB ("Multiply Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Progress;
            float _GridSize;
            float _CornerRadius;
            float4 _ColorA;
            float4 _ColorB;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 screenPos : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex).xy;
                return o;
            }

            float roundedBox(float2 pos, float2 center, float2 halfSize, float radius)
            {
                float2 q = abs(pos - center) - halfSize + radius;
                float inside = step(length(max(q, 0.0)), radius);
                return inside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pixelPos = i.screenPos * _ScreenParams.xy;
                float2 grid = floor(pixelPos / _GridSize);

                // 계산된 최대 revealOrder
                float maxOrder = (_ScreenParams.x / _GridSize) + (_ScreenParams.y / _GridSize);
                // 0~1로 정규화
                float revealOrder = (grid.x + (_ScreenParams.y / _GridSize - grid.y)) / maxOrder;
                
                float tAppear = smoothstep(0.0, 1.0, _Progress - revealOrder);
                float tDisappear = smoothstep(3.0, 2.0, _Progress - revealOrder); // 역방향으로 사라짐
                float t = min(tAppear, tDisappear); // 둘 다 1이어야 유지됨
                //float t = smoothstep(0.0, 1.0, _Progress - revealOrder);

                float scale = smoothstep(0.0, 1.0, t);

                float2 centerOfCell = (grid + 0.5) * _GridSize;
                float2 halfSize = (_GridSize * 0.5) * scale;

                // Radius shrinks as t approaches 1
                float radius = _GridSize * _CornerRadius * scale * (1.0 - scale);

                float inside = roundedBox(pixelPos, centerOfCell, halfSize, radius);
                float4 texColor = tex2D(_MainTex, i.uv) * _ColorB;
                return lerp(_ColorA, texColor, inside);
            }
            ENDCG
        }
    }
}

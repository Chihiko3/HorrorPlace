Shader "Unlit/ShaderTest"
{
    Properties
    {
        _ColorA("Color", Color) = (1,1,1,1)
        _ColorB("Color", Color) = (1,1,1,1)
        _StartPoint("Start Point", Range(0, 1)) = 0
        _EndPoint("End Point", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _ColorA;
            float4 _ColorB;
            float _StartPoint;
            float _EndPoint;

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv0 : TEXCOORD0;

            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };


            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv0;
                return o;
            }

            fixed4 frag (Interpolators i) : SV_Target
            {
                float4 outColor = lerp(_ColorA, _ColorB, i.uv.y);

                return outColor;
            }
            ENDCG
        }
    }
}

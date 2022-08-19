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
        Tags 
        { 
            "RenderType"="Transparent"  // tag to inform the render pipeline of what ype this is 
            "Queue"="Transparent" // changes the render order
            
        }

        Pass
        {
            Cull Off
            Zwrite Off
            ZTest LEqual
            Blend One One // additive
            
            
            // Blend DstColor Zero // multiply
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define TAU 6.28

            float4 _ColorA;
            float4 _ColorB;
            float _StartPoint;
            float _EndPoint;

            struct MeshData // data that will be used in vertex shader
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv0 : TEXCOORD0;

            };

            struct Interpolators // data that will be used in fragment shader
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };


            Interpolators vert (MeshData v) // pass vertex data to fragment data
            {
                
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex); // local space to clip space
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv0; // (v.uv.0 + _Offset) * _Scale; 
                return o;
            }

            float Lerp(float a, float b, float t)
            {
                return (1.0f - t) * a + b * t;
            }
            
            float InverseLerp(float a, float b, float v) //like a new method in c#
            {
                return (v-a)/(b-a);
            }

            float Remap(float iMin, float iMax, float oMin, float oMax, float v)
            {
                float t = InverseLerp(iMin, iMax, v);
                return Lerp(oMin, oMax, t);
            }


            float4 frag (Interpolators i) : SV_Target
            {
                float xOffset = cos(i.uv.x * TAU * 2) * 0.01;
                float t = cos((i.uv.y + xOffset - _Time.y * 0.2) * 5 * TAU) * 0.5 + 0.5;
                t *= 1 - i.uv.y;

                float topBottonRemover = (abs(i.normal.y) < 0.999);
                float waves = t * topBottonRemover;

                float4 gradient = lerp(_ColorA, _ColorB, i.uv.y);

                
                return gradient * waves;
                
            }
            ENDCG
        }
    }
}
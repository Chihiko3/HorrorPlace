Shader "Unlit/VertexOffset"
{
    Properties
    {
        _ColorA("Color", Color) = (1,1,1,1)
        _ColorB("Color", Color) = (1,1,1,1)
        _StartPoint("Start Point", Range(0, 1)) = 0
        _EndPoint("End Point", Range(0, 1)) = 1
        _WaveAmp("Wave Amplitude", Range(0, 0.2)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"  // tag to inform the render pipeline of what ype this is 
            "Queue"="Geometry" // changes the render order
            
        }

        Pass
        {
            
            
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
            float _WaveAmp;

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

            float GetWave( float2 uv)
            {
                float2 uvsCentered = uv * 2 - 1; 
                float radioDistance = length(uvsCentered);
                //return float4(radioDistance.xxx , 1);
                
                float wave = cos((radioDistance - _Time.y * 0.1) * 5 * TAU) * 0.5 + 0.5;
                wave *= 1 - radioDistance;
                return wave;
            }

            Interpolators vert (MeshData v) // pass vertex data to fragment data
            {
                Interpolators o;

                v.vertex.y = GetWave(v.uv0) * _WaveAmp;

                //float wave = cos((v.uv0.y - _Time.y * 0.1) * 5 * TAU + 3.5);
                //float wave2 = cos((v.uv0.x - _Time.y * 0.1) * 5 * TAU);

                //v.vertex.y = wave * _WaveAmp;
                

                o.vertex = UnityObjectToClipPos(v.vertex); 
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv0;
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
                return GetWave(i.uv);
                
            }
            ENDCG
        }
    }
}

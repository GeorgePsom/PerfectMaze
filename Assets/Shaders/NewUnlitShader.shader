Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
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
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_INSTANCEID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                LIGHTING_COORDS(2, 3)
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            #define UNITY_INSTANCING_ENABLED
            #ifdef SHADER_API_D3D11
                        StructuredBuffer<float4> _Offsets;
            #endif
            float3 _HorizontalScale;
            float3 _VerticalScale;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                float3 offset = _Offsets[v.instanceID].xyz;
                float3 scale = _Offsets[v.instanceID].w > 0 ? _HorizontalScale : _VerticalScale;
                /*float4x4 worldSpaceMatrix = float4x4 (
                    scale.x, 0, 0, 0,
                    0, scale.y, 0, 0,
                    0, 0, scale.z, 0,
                    offset.x, offset.y, offset.z, 1
                    );*/
                float4x4 worldSpaceMatrix = float4x4(
                    scale.x, 0, 0, offset.x,
                    0, scale.y, 0, offset.y,
                    0, 0, scale.z, offset.z,
                    0, 0, 0, 1
                    );
                float4 pos = mul(worldSpaceMatrix, v.vertex);

                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                //TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                // apply fog
                float atten = LIGHT_ATTENUATION(i);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * atten;
            }
                ENDCG
        }

            Pass
            {
                    Tags{"LightMode" = "ShadowCaster"}
                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragShadow
                    #pragma target 2.0
                    #pragma multi_compile_shadowcaster
                    #include "UnityCG.cginc"
                    #include "AutoLight.cginc"


                /*v2fShadow vertShadow(appdata_base v)
                {
                    v2fShadow o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                    return o;
                }*/
                 struct v2f
                {
                    float2 uv : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                };
                
                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    uint instanceID : SV_INSTANCEID;
                };
                #define UNITY_INSTANCING_ENABLED
                #ifdef SHADER_API_D3D11
                            StructuredBuffer<float4> _Offsets;
                #endif
                float3 _HorizontalScale;
                float3 _VerticalScale;

                v2f vert(appdata v)
                {
                    v2f o;
                    float3 offset = _Offsets[v.instanceID].xyz;
                    float3 scale = _Offsets[v.instanceID].w > 0 ? _HorizontalScale : _VerticalScale;
                    /*float4x4 worldSpaceMatrix = float4x4 (
                        scale.x, 0, 0, 0,
                        0, scale.y, 0, 0,
                        0, 0, scale.z, 0,
                        offset.x, offset.y, offset.z, 1
                        );*/
                    float4x4 worldSpaceMatrix = float4x4(
                        scale.x, 0, 0, offset.x,
                        0, scale.y, 0, offset.y,
                        0, 0, scale.z, offset.z,
                        0, 0, 0, 1
                        );
                    float4 pos = mul(worldSpaceMatrix, v.vertex);

                    o.vertex = mul(UNITY_MATRIX_VP, pos);
                    o.uv = v.uv;
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    //TRANSFER_VERTEX_TO_FRAGMENT(o);
                    return o;
                }

                float4 fragShadow(v2f i) : SV_Target
                {
                    return 0;
                }
                ENDCG

        }
    }
}

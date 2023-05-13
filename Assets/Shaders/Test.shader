Shader "Custom/IndirectInstanced"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        [Gamma] _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.1
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
            #pragma multi_compile_fwdadd
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "UnityPBSLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_INSTANCEID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                
            };

            
            #define UNITY_INSTANCING_ENABLED
            #ifdef SHADER_API_D3D11
                        StructuredBuffer<float4> _Offsets;
            #endif
            float3 _HorizontalScale;
            float3 _VerticalScale;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Smoothness;
            float _Metallic;

            v2f vert (appdata v)
            {
                v2f o;
                float3 offset = _Offsets[v.instanceID].xyz;
                float3 scale = _Offsets[v.instanceID].w > 0 ? _HorizontalScale : _VerticalScale;
              
                float4x4 worldSpaceMatrix = float4x4(
                    scale.x, 0, 0, offset.x,
                    0, scale.y, 0, offset.y,
                    0, 0, scale.z, offset.z,
                    0, 0, 0, 1
                    );
                o.worldPos = mul(worldSpaceMatrix, v.vertex).xyz;
                o.normal = normalize(
                    worldSpaceMatrix[0].xyz * v.normal.x +
                    worldSpaceMatrix[1].xyz * v.normal.y +
                    worldSpaceMatrix[2].xyz * v.normal.z
                );
                o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPos, 1));
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
               
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {

                
               i.normal = normalize(i.normal);
              
               float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
               float3 lightColor = _LightColor0.rgb;
               float3 albedo = tex2D(_MainTex, i.uv) *  _Color.rgb;
              
               float3 specular;
               float oneMinusReflectivity;
               albedo = DiffuseAndSpecularFromMetallic(
                   albedo, _Metallic, specular, oneMinusReflectivity);

               UnityLight light;

               light.dir = _WorldSpaceLightPos0.xyz;

               UNITY_LIGHT_ATTENUATION(attenuation, 0, i.worldPos);
               light.color = lightColor * attenuation;
               light.ndotl = DotClamped(i.normal, light.dir);
               UnityIndirect indirectLight;
               indirectLight.diffuse = 0;
               indirectLight.specular = 0;

               float4 result = UNITY_BRDF_PBS(
                   albedo, specular, oneMinusReflectivity, _Smoothness,
                   i.normal, viewDir, light, indirectLight
               );

               return result;
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

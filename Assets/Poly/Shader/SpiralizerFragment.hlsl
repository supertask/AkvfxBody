// Spiralizer effect fragment shader
// https://github.com/keijiro/TestbedHDRP

/*
half4 _EffHSVM;
half4 _EdgeHSVM;
half _EdgeWidth;
half _HueShift;

// Self emission term (effect emission + edge detection)
half3 SelfEmission(FragInputs input)
{
    half tape = input.color.x;
    half intensity = input.color.y; // 0:off -> 1:emission -> 2:edge
    half random = input.color.z;

    // Edge detection
    half edge = min(tape, 1 - tape) > fwidth(tape) * _EdgeWidth;
    edge = (1 - edge) * (tape >= 0);

    // Random hue shift
    half hueShift = (random - 0.5) * 2 * _HueShift;

    // Emission color
    half3 c1 = HsvToRgb(half3(_EffHSVM.x + hueShift, _EffHSVM.yz));
    c1 *= _EffHSVM.w * saturate(intensity);

    // Edge color
    half3 c2 = HsvToRgb(half3(_EdgeHSVM.x + hueShift, _EdgeHSVM.yz));
    c2 *= edge * _EdgeHSVM.w;

    return lerp(c1, c2, saturate(intensity - 1));
}
*/

// Fragment shader function, copy-pasted from HDRP/ShaderPass/ShaderPassGBuffer.hlsl
// There are a few modification from the original shader. See "Custom:" for details.
void SpiralizerFragment(
            PackedVaryingsToPS packedInput,
            OUTPUT_GBUFFER(outGBuffer)
            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
            )
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // Custom: Cancel the normal map while the effect is active.
    //float cancel = saturate(input.color.y);
    surfaceData.baseColor = float3(0,1,0);
    //surfaceData.normalWS = surfaceData.normalWS

    // Custom: Add the self emission term.
    //builtinData.bakeDiffuseLighting += SelfEmission(input);

    ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}

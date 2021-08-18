using System;

namespace ValveResourceFormat.ResourceTypes.ModelAnimation
{
    public enum AnimDecoderType
    {
        Unknown,
        CCompressedReferenceFloat,
        CCompressedStaticFloat,
        CCompressedFullFloat,
        CCompressedReferenceVector3,
        CCompressedStaticVector3,
        CCompressedStaticFullVector3,
        CCompressedAnimVector3,
        CCompressedDeltaVector3,
        CCompressedFullVector3,
        CCompressedReferenceQuaternion,
        CCompressedStaticQuaternion,
        CCompressedAnimQuaternion,
        CCompressedFullQuaternion,
        CCompressedReferenceInt,
        CCompressedStaticChar,
        CCompressedFullChar,
        CCompressedStaticShort,
        CCompressedFullShort,
        CCompressedStaticInt,
        CCompressedFullInt,
        CCompressedReferenceBool,
        CCompressedStaticBool,
        CCompressedFullBool,
        CCompressedReferenceColor32,
        CCompressedStaticColor32,
        CCompressedFullColor32,
        CCompressedReferenceVector2D,
        CCompressedStaticVector2D,
        CCompressedFullVector2D,
        CCompressedReferenceVector4D,
        CCompressedStaticVector4D,
        CCompressedFullVector4D,
    }
}

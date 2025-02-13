using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Horus.Modules.Core.Infra.Context;

public class VectorConverter : ValueConverter<float[], byte[]>
{
    public VectorConverter()
        : base(
            v => ConvertToDatabase(v), // Convert to byte[]
            v => ConvertFromDatabase(v) // Convert back to float[]
        )
    {
    }

    // Convert float[] to byte[]
    private static byte[] ConvertToDatabase(float[] value)
    {
        if (value == null) return null;
        var byteArray = new byte[value.Length * sizeof(float)];
        Buffer.BlockCopy(value, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }

    // Convert byte[] to float[]
    private static float[] ConvertFromDatabase(byte[] value)
    {
        if (value == null) return null;
        var floatArray = new float[value.Length / sizeof(float)];
        Buffer.BlockCopy(value, 0, floatArray, 0, value.Length);
        return floatArray;
    }
}
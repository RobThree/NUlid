using System;
using System.ComponentModel;
using System.Globalization;

namespace NUlid
{
    /// <summary>
    /// Ulid Type Converter.
    /// </summary>
    internal sealed class UlidTypeConverter
        : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(byte[])
                || sourceType == typeof(string)                
                || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(byte[])
                || destinationType == typeof(string)
                || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var asByteArray = value as byte[];
            var asString = value as string;

            if(asByteArray != null)
            {
                return new Ulid(asByteArray);
            }

            if(asString != null)
            {
                Ulid ulid;
                if (Ulid.TryParse(asString, out ulid)) return ulid;
                throw new NotSupportedException($"Invalid Ulid representation: \"{asString}\"");
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if(destinationType == typeof(byte[]))
            {
                return ((Ulid)value).ToByteArray();
            }

            if (destinationType == typeof(string))
            {
                return ((Ulid)value).ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
